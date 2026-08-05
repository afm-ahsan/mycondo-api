using FluentValidation;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Queries.GetStaffMembersForTenant;

public sealed class GetStaffMembersForTenantQueryValidator : AbstractValidator<GetStaffMembersForTenantQuery>
{
    public GetStaffMembersForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
