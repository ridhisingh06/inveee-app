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
        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }
}

