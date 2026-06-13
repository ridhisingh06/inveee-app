variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "ap-south-1"
}

variable "db_password" {
  description = "Database password"
  type        = string
  default     = "ridhi@608"
  sensitive   = true
}
