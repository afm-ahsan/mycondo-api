using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MyCondo.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Stopwatch sw = Stopwatch.StartNew();
        TResponse response = await next(cancellationToken);
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Slow request {RequestName} took {ElapsedMs} ms",
                typeof(TRequest).Name, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
