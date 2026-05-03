using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using MyCondo.Domain.Exceptions;
using AppEx = MyCondo.Application.Common.Exceptions.ApplicationException;

namespace MyCondo.Application.Common.Behaviors;

public sealed class UnhandledExceptionBehavior<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
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
                typeof(TRequest).Name, ex.Message);
            throw;
        }
    }
}
