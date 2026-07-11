terraform {
  required_version = ">= 1.0.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# Default VPC and Subnets
data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

# ECR Repository
resource "aws_ecr_repository" "app" {
  name                 = "inveee-app"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

# Security Group for ECS
resource "aws_security_group" "ecs_sg" {
  name        = "inveee-ecs-sg"
  description = "Allow HTTP inbound traffic"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description = "HTTP from anywhere"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# Additional ingress rule for app port
resource "aws_security_group_rule" "ecs_app_port" {
  type              = "ingress"
  from_port         = 5000
  to_port           = 5000
  protocol          = "tcp"
  cidr_blocks       = ["0.0.0.0/0"]
  security_group_id = aws_security_group.ecs_sg.id
}

# RDS DB Subnet Group
resource "aws_db_subnet_group" "default" {
  name       = "inveee-db-subnet-group"
  subnet_ids = data.aws_subnets.default.ids

  tags = {
    Name = "inveee-db-subnet-group"
  }
}

# Security Group for RDS
resource "aws_security_group" "rds_sg" {
  name        = "inveee-rds-sg"
  description = "Allow PostgreSQL inbound traffic"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description     = "PostgreSQL from ECS"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs_sg.id]
  }

  ingress {
    description = "PostgreSQL from anywhere (for publicly accessible)"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# KMS Key for RDS Encryption
resource "aws_kms_key" "rds_key" {
  description             = "KMS key for RDS encryption"
  deletion_window_in_days = 10
  enable_key_rotation     = true

  tags = {
    Name = "inveee-rds-key"
  }
}

resource "aws_kms_alias" "rds_key_alias" {
  name          = "alias/inveee-rds"
  target_key_id = aws_kms_key.rds_key.key_id
}

# IAM Role for RDS Monitoring
resource "aws_iam_role" "rds_monitoring_role" {
  name = "inveee-rds-monitoring-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "monitoring.rds.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy" "rds_monitoring_policy" {
  name = "inveee-rds-monitoring-policy"
  role = aws_iam_role.rds_monitoring_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:${var.aws_region}:*:log-group:/aws/rds/*"
      },
      {
        Effect = "Allow"
        Action = [
          "cloudwatch:PutMetricAlarm",
          "cloudwatch:PutMetricData"
        ]
        Resource = "*"
      }
    ]
  })
}

# RDS Instance
resource "aws_db_instance" "postgres" {
  identifier                = "inveee-postgres"
  engine                    = "postgres"
  engine_version            = "18.3"
  instance_class            = "db.t3.micro"
  allocated_storage         = 20
  db_name                   = "inventorydb"
  username                  = "postgres"
  password                  = var.db_password
  publicly_accessible       = true
  skip_final_snapshot       = false
  final_snapshot_identifier = "inveee-postgres-final-snapshot-${formatdate("YYYY-MM-DD-hhmm", timestamp())}"
  vpc_security_group_ids    = [aws_security_group.rds_sg.id]
  db_subnet_group_name      = aws_db_subnet_group.default.name

  # Backup Configuration
  backup_retention_period = 30
  backup_window           = "03:00-04:00"
  maintenance_window      = "mon:04:00-mon:05:00"

  # Enhanced Monitoring
  monitoring_interval             = 60
  monitoring_role_arn             = aws_iam_role.rds_monitoring_role.arn
  enabled_cloudwatch_logs_exports = ["postgresql"]

  # Encryption
  storage_encrypted = true
  kms_key_id        = aws_kms_key.rds_key.arn

  # Performance Insights
  performance_insights_enabled          = true
  performance_insights_retention_period = 7

  tags = {
    Name = "inveee-postgres-prod"
  }

  depends_on = [aws_iam_role_policy.rds_monitoring_policy]
}

# ECS Cluster
resource "aws_ecs_cluster" "main" {
  name = "inveee-cluster"
}

# IAM Role for ECS Task Execution
resource "aws_iam_role" "ecs_task_execution_role" {
  name = "inveee-task-execution-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# Add CloudWatch Logs policy for ECS tasks
resource "aws_iam_role_policy" "ecs_task_execution_role_logs" {
  name = "ecs-task-execution-logs"
  role = aws_iam_role.ecs_task_execution_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:${var.aws_region}:*:log-group:/ecs/*"
      }
    ]
  })
}

# ECS Task Definition
resource "aws_ecs_task_definition" "app" {
  family                   = "inveee-app-task"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = 256
  memory                   = 512
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn

  container_definitions = jsonencode([
    {
      name      = "app"
      image     = "${aws_ecr_repository.app.repository_url}:latest"
      essential = true
      portMappings = [
        {
          containerPort = 5000
          hostPort      = 5000
          protocol      = "tcp"
        }
      ]
      environment = [
        {
          name  = "ConnectionStrings__DefaultConnection"
          value = "Host=${aws_db_instance.postgres.address};Port=5432;Database=inventorydb;Username=postgres;Password=${var.db_password};Pooling=true;Minimum Pool Size=5;Maximum Pool Size=20;Connection Idle Lifetime=30;SSL Mode=Prefer;Trust Server Certificate=true;"
        },
        {
          name  = "ASPNETCORE_ENVIRONMENT"
          value = "Production"
        },
        {
          name  = "ASPNETCORE_URLS"
          value = "http://+:5000"
        }
      ]
      healthCheck = {
        command     = ["CMD-SHELL", "curl -f http://localhost:5000/health || exit 1"]
        interval    = 30
        timeout     = 10
        retries     = 3
        startPeriod = 60
      }
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = "/ecs/inveee-app"
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "ecs"
          "awslogs-create-group"  = "true"
        }
      }
    }
  ])
}

# ECS Service
resource "aws_ecs_service" "app" {
  name            = "inveee-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.app.arn
  desired_count   = 2
  launch_type     = "FARGATE"
  depends_on      = [aws_db_instance.postgres]

  network_configuration {
    subnets          = ["subnet-021e42ebc310fde8f", "subnet-0a4a3b2d8b3e9f185"]
    security_groups  = [aws_security_group.ecs_sg.id]
    assign_public_ip = true
  }
}

# Auto Scaling Target for ECS Service
resource "aws_appautoscaling_target" "ecs_target" {
  max_capacity       = 10
  min_capacity       = 2
  resource_id        = "service/${aws_ecs_cluster.main.name}/${aws_ecs_service.app.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

# CPU-based Scaling Policy
resource "aws_appautoscaling_policy" "ecs_policy_cpu" {
  name               = "inveee-cpu-autoscaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.ecs_target.resource_id
  scalable_dimension = aws_appautoscaling_target.ecs_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.ecs_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value = 70.0
  }
}

# Memory-based Scaling Policy
resource "aws_appautoscaling_policy" "ecs_policy_memory" {
  name               = "inveee-memory-autoscaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.ecs_target.resource_id
  scalable_dimension = aws_appautoscaling_target.ecs_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.ecs_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageMemoryUtilization"
    }
    target_value = 80.0
  }
}
