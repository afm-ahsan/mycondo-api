using FluentValidation;

namespace MyCondo.Application.Features.Security.Vehicles.Queries.GetVehiclesForTenant;

public sealed class GetVehiclesForTenantQueryValidator : AbstractValidator<GetVehiclesForTenantQuery>
{
    public GetVehiclesForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
