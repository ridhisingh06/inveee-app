# =============================================================================
# CloudFront Distribution for inveee-app API
#
# This resource mirrors the manually-created distribution dh8mq54lnbssr.cloudfront.net
# (Distribution ID: E3VRYF1FMD8JQX).
#
# Import command:
#   terraform import aws_cloudfront_distribution.api_cdn E3VRYF1FMD8JQX
#
# Current config (verified via AWS CLI 2026-07-11):
#   - Origin: inveee-alb-503765841.us-east-1.elb.amazonaws.com (HTTP-only)
#   - Cache Policy: Managed-CachingDisabled (TTL=0) ✅
#   - Origin Request Policy: Managed-AllViewerExceptHostHeader ✅
#   - Allowed Methods: all 7 (GET,HEAD,POST,PUT,PATCH,DELETE,OPTIONS) ✅
#   - Viewer Protocol: redirect-to-https ✅
# =============================================================================

# ── Managed Policy Data Sources ───────────────────────────────────────────────
# Reference the AWS-managed policies already attached to the distribution.
# These are the correct policies for an API backend that handles CORS itself.

data "aws_cloudfront_cache_policy" "caching_disabled" {
  name = "Managed-CachingDisabled"
}

data "aws_cloudfront_origin_request_policy" "all_viewer_except_host" {
  name = "Managed-AllViewerExceptHostHeader"
}

# ── CloudFront Distribution ────────────────────────────────────────────────────

resource "aws_cloudfront_distribution" "api_cdn" {
  enabled         = true
  is_ipv6_enabled = true
  comment         = "inveee-app API CDN — routes all traffic to ECS via ALB"
  price_class     = "PriceClass_All"
  http_version    = "http2"

  # Origin: ALB in front of ECS Fargate tasks
  origin {
    domain_name = "inveee-alb-503765841.us-east-1.elb.amazonaws.com"
    origin_id   = "inveee-alb-503765841.us-east-1.elb.amazonaws.com-mr5btjrvaws"

    custom_origin_config {
      http_port                = 80
      https_port               = 443
      origin_protocol_policy   = "http-only" # ALB terminates TLS, forwards HTTP to ECS:5000
      origin_ssl_protocols     = ["TLSv1.2"]
      origin_read_timeout      = 30
      origin_keepalive_timeout = 5
    }
  }

  # Default behavior — pass everything to ALB, no caching
  default_cache_behavior {
    allowed_methods        = ["DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "inveee-alb-503765841.us-east-1.elb.amazonaws.com-mr5btjrvaws"
    viewer_protocol_policy = "redirect-to-https"
    compress               = true

    # Managed-CachingDisabled: TTL=0, no caching at all
    cache_policy_id = data.aws_cloudfront_cache_policy.caching_disabled.id

    # Managed-AllViewerExceptHostHeader: forwards all request headers except Host
    # This ensures Origin, Access-Control-Request-Method, Access-Control-Request-Headers,
    # Authorization, Content-Type, etc. all reach the .NET backend for CORS evaluation
    origin_request_policy_id = data.aws_cloudfront_origin_request_policy.all_viewer_except_host.id
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
    minimum_protocol_version       = "TLSv1"
  }

  tags = {
    Name        = "inveee-api-cdn"
    Environment = "production"
  }
}

# ── Outputs ───────────────────────────────────────────────────────────────────

output "cloudfront_domain" {
  description = "CloudFront distribution domain — used as apiUrl in Angular environments"
  value       = "https://${aws_cloudfront_distribution.api_cdn.domain_name}"
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID"
  value       = aws_cloudfront_distribution.api_cdn.id
}
