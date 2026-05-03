using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace MyCondo.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TMessage, TResponse>(
    ILogger<PerformanceBehavior<TMessage, TResponse>> logger
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private const int SlowRequestThresholdMs = 500;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        Stopwatch sw = Stopwatch.StartNew();
        TResponse response = await next(message, cancellationToken);
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Slow request {RequestName} took {ElapsedMs} ms",
                typeof(TMessage).Name, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
