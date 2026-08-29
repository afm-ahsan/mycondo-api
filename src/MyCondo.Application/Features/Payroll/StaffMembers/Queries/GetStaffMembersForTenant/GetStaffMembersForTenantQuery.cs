using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payroll.StaffMembers.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Queries.GetStaffMembersForTenant;

public sealed record GetStaffMembersForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<StaffMemberDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "security.staff_attendance";
}
