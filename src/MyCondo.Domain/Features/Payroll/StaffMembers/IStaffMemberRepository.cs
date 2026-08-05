using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Payroll.StaffMembers;

public interface IStaffMemberRepository
{
    Task<StaffMember?> GetByIdAsync(StaffMemberId id, CancellationToken cancellationToken);

    Task<PagedResult<StaffMember>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(StaffMember staffMember);
}
