
# =============================================================================
# CloudFront Distribution for inveee-app API
#
# CRITICAL: This distribution MUST forward the Origin, Access-Control-Request-Method,
# and Access-Control-Request-Headers headers to the backend so that .NET's CORS
# middleware can evaluate them and return the correct Access-Control-Allow-* headers.
#
# The existing manually-created distribution (dh8mq54lnbssr.cloudfront.net) should
# be imported or replaced with this resource.
#
# To import the existing distribution:
#   terraform import aws_cloudfront_distribution.api_cdn dh8mq54lnbssr
# =============================================================================

# ── Variables ─────────────────────────────────────────────────────────────────

variable "alb_dns_name" {
  description = "DNS name of the Application Load Balancer (or ECS public IP for simple setups)"
  type        = string
  # Set this to your ALB DNS or the ECS service public IP
  # e.g. "inveee-alb-1234567890.us-east-1.elb.amazonaws.com"
  default = ""
}

# ── Origin Request Policy — forward CORS headers ──────────────────────────────
#
# CloudFront must forward these three headers to the origin (ECS/.NET):
#   - Origin                          (required for CORS evaluation)
#   - Access-Control-Request-Method   (required for preflight)
#   - Access-Control-Request-Headers  (required for preflight)
#
# Without this policy, CloudFront strips those headers and .NET never sees the
# CORS request → returns no Access-Control-Allow-* headers → browser blocks.

resource "aws_cloudfront_origin_request_policy" "cors_api" {
  name    = "inveee-cors-origin-request-policy"
  comment = "Forward CORS and common API headers to the .NET backend"

  cookies_config {
    cookie_behavior = "none"
  }

  headers_config {
    header_behavior = "whitelist"
    headers {
      items = [
        "Origin",
        "Access-Control-Request-Method",
        "Access-Control-Request-Headers",
        "Authorization",
        "Content-Type",
        "Accept",
        "X-Requested-With",
      ]
    }
  }

  query_strings_config {
    query_string_behavior = "all"
  }
}

# ── Cache Policy — no caching for API / preflight ─────────────────────────────
#
# Preflight (OPTIONS) responses must NEVER be cached by CloudFront.
# A cached preflight for origin A will be served for origin B → CORS fail.
# Use TTL=0 for all API routes.

resource "aws_cloudfront_cache_policy" "api_no_cache" {
  name        = "inveee-api-no-cache"
  comment     = "Zero-TTL policy for API routes — prevents stale CORS preflight caching"
  default_ttl = 0
  max_ttl     = 0
  min_ttl     = 0

  parameters_in_cache_key_and_forwarded_to_origin {
    enable_accept_encoding_brotli = false
    enable_accept_encoding_gzip   = false

    cookies_config {
      cookie_behavior = "none"
    }

    headers_config {
      header_behavior = "none"
    }

    query_strings_config {
      query_string_behavior = "none"
    }
  }
}

# ── CloudFront Distribution ────────────────────────────────────────────────────

resource "aws_cloudfront_distribution" "api_cdn" {
  enabled         = true
  is_ipv6_enabled = true
  comment         = "inveee-app API CDN — routes /api/* to ECS backend"
  price_class     = "PriceClass_100" # US/EU only — cheapest

  # Origin: ECS backend (ALB or direct ECS public IP)
  origin {
    domain_name = var.alb_dns_name != "" ? var.alb_dns_name : "REPLACE_WITH_ALB_OR_ECS_DNS"
    origin_id   = "inveee-ecs-origin"

    custom_origin_config {
      http_port              = 5000
      https_port             = 443
      origin_protocol_policy = "http-only" # ECS listens on HTTP:5000, TLS is terminated at CloudFront
      origin_ssl_protocols   = ["TLSv1.2"]

      # Keep connections alive to avoid TCP handshake overhead per request
      origin_keepalive_timeout = 60
      origin_read_timeout      = 60
    }
  }

  # ── Default behaviour — deny direct access to root ────────────────────────
  default_cache_behavior {
    allowed_methods        = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "inveee-ecs-origin"
    viewer_protocol_policy = "redirect-to-https"

    cache_policy_id            = aws_cloudfront_cache_policy.api_no_cache.id
    origin_request_policy_id   = aws_cloudfront_origin_request_policy.cors_api.id

    compress = true
  }

  # ── /api/* — all methods including OPTIONS must reach the .NET backend ────
  ordered_cache_behavior {
    path_pattern           = "/api/*"
    allowed_methods        = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods         = ["GET", "HEAD", "OPTIONS"]
    target_origin_id       = "inveee-ecs-origin"
    viewer_protocol_policy = "redirect-to-https"

    # No caching: TTL = 0 so every OPTIONS preflight reaches the .NET middleware
    cache_policy_id          = aws_cloudfront_cache_policy.api_no_cache.id
    origin_request_policy_id = aws_cloudfront_origin_request_policy.cors_api.id

    compress = true
  }

  # ── /health — direct health check passthrough ────────────────────────────
  ordered_cache_behavior {
    path_pattern           = "/health"
    allowed_methods        = ["GET", "HEAD"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "inveee-ecs-origin"
    viewer_protocol_policy = "redirect-to-https"

    cache_policy_id          = aws_cloudfront_cache_policy.api_no_cache.id
    origin_request_policy_id = aws_cloudfront_origin_request_policy.cors_api.id

    compress = true
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }

  tags = {
    Name        = "inveee-api-cdn"
    Environment = "production"
  }
}

# ── Outputs ───────────────────────────────────────────────────────────────────

output "cloudfront_domain" {
  description = "CloudFront distribution domain — use this as apiUrl in Angular environments"
  value       = "https://${aws_cloudfront_distribution.api_cdn.domain_name}"
}
