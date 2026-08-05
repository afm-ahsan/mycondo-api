using Mediator;
using MyCondo.Application.Features.Payroll.StaffMembers.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Queries.GetStaffMembersForTenant;

public sealed record GetStaffMembersForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<StaffMemberDto>>;
