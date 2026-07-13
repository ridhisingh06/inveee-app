using Serilog.Context;

namespace invmgmt.web.Utils;

public sealed class TraceIdEnricherMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdEnricherMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var traceId = context.TraceIdentifier;
        using var prop1 = LogContext.PushProperty("TraceId", traceId);

        IDisposable? prop2 = null;
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
            {
                prop2 = LogContext.PushProperty("UserId", userId);
            }
        }

        IDisposable? prop3 = null;
        var routeValues = context.Request.RouteValues;
        if (routeValues.TryGetValue("id", out var idObj) || routeValues.TryGetValue("requestId", out idObj))
        {
            if (idObj != null)
            {
                prop3 = LogContext.PushProperty("RequestId", idObj.ToString());
            }
        }

        try
        {
            await _next(context);
        }
        finally
        {
            prop2?.Dispose();
            prop3?.Dispose();
        }
    }
}

