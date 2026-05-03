using Mediator;
using Microsoft.Extensions.Logging;

namespace MyCondo.Application.Common.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse>(
    ILogger<LoggingBehavior<TMessage, TResponse>> logger
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TMessage).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        TResponse response = await next(message, cancellationToken);

        logger.LogInformation("Handled {RequestName}", requestName);
        return response;
    }
}
