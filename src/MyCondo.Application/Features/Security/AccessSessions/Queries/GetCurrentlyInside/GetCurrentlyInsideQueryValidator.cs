using FluentValidation;
using MyCondo.Domain.Features.Security.AccessSessions;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetCurrentlyInside;

public sealed class GetCurrentlyInsideQueryValidator : AbstractValidator<GetCurrentlyInsideQuery>
{
    public GetCurrentlyInsideQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Category).Must(BeAValidCategory!).When(x => x.Category is not null)
            .WithMessage($"Category must be one of: {string.Join(", ", Enum.GetNames<AccessCategory>())}.");
    }

    private static bool BeAValidCategory(string value) => Enum.TryParse<AccessCategory>(value, out _);
}
