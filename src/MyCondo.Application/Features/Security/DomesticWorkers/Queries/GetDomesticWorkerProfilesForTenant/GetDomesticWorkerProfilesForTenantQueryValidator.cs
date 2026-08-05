using FluentValidation;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Queries.GetDomesticWorkerProfilesForTenant;

public sealed class GetDomesticWorkerProfilesForTenantQueryValidator : AbstractValidator<GetDomesticWorkerProfilesForTenantQuery>
{
    public GetDomesticWorkerProfilesForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
