using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payroll.StaffMembers.DTOs;
using MyCondo.Application.Features.Payroll.StaffMembers.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Queries.GetStaffMembersForTenant;

public sealed class GetStaffMembersForTenantQueryHandler(
    IStaffMemberRepository staffMembers,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetStaffMembersForTenantQuery, PagedResult<StaffMemberDto>>
{
    public async ValueTask<PagedResult<StaffMemberDto>> Handle(GetStaffMembersForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<StaffMember> result = await staffMembers.SearchAsync(
            tenantId, query.Search, query.Page, query.PageSize, cancellationToken);

        List<StaffMemberDto> items = result.Items.Select(s => s.ToDto()).ToList();

        return new PagedResult<StaffMemberDto>(items, result.Page, result.PageSize, result.Total);
    }
}
