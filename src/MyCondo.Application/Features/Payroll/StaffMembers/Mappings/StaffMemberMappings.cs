using MyCondo.Application.Features.Payroll.StaffMembers.DTOs;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Mappings;

internal static class StaffMemberMappings
{
    public static StaffMemberDto ToDto(this StaffMember staffMember) => new(
        staffMember.Id.Value, staffMember.FullName, staffMember.Role.ToString(), staffMember.Phone, staffMember.IsActive);
}
