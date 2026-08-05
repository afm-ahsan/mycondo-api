using FluentValidation;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Queries.GetParcelsForTenant;

public sealed class GetParcelsForTenantQueryValidator : AbstractValidator<GetParcelsForTenantQuery>
{
    public GetParcelsForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(BeAValidStatus!).When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<ParcelStatus>())}.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<ParcelStatus>(value, out _);
}
