using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Domain.Exceptions;
using AppEx = MyCondo.Application.Common.Exceptions.ApplicationException;

namespace MyCondo.Application.Common.Behaviors;

public sealed class UnhandledExceptionBehavior<TMessage, TResponse>(
    ILogger<UnhandledExceptionBehavior<TMessage, TResponse>> logger
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(message, cancellationToken);
        }
        catch (Exception ex) when (ex is ValidationException
                                      or AppEx
                                      or DomainException)
        {
            // Expected business exceptions: let them bubble to GlobalExceptionMiddleware untouched.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception for request {RequestName}: {Message}",
                typeof(TMessage).Name, ex.Message);
            throw;
        }
    }
}
