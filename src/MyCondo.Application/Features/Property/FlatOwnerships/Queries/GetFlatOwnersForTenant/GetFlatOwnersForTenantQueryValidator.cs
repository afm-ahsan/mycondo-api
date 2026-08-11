using FluentValidation;
using MyCondo.Domain.Features.Property.FlatOwnerships;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnersForTenant;

public sealed class GetFlatOwnersForTenantQueryValidator : AbstractValidator<GetFlatOwnersForTenantQuery>
{
    public GetFlatOwnersForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(BeAValidStatus!).When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<FlatOwnershipStatus>())}.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<FlatOwnershipStatus>(value, out _);
}
