using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace MyCondo.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        ValidationContext<TRequest> context = new(request);
        ValidationResult[] results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<ValidationFailure> failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
