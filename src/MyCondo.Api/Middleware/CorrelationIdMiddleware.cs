using Serilog.Context;

namespace MyCondo.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        string id = context.Request.Headers[Header].FirstOrDefault()
            ?? Guid.CreateVersion7().ToString();

        context.Response.Headers[Header] = id;

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(context);
        }
    }
}
