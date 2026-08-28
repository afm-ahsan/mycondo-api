using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payroll.StaffMembers.DTOs;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Commands.RegisterStaffMember;

public sealed record RegisterStaffMemberCommand(string FullName, string Role, string? Phone) : IRequest<StaffMemberDto>, IRequiresFeature
{
    public string FeatureKey => "security.staff_attendance";
}
