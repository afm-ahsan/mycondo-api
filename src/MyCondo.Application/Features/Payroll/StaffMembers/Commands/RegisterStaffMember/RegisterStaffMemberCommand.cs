using Mediator;
using MyCondo.Application.Features.Payroll.StaffMembers.DTOs;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Commands.RegisterStaffMember;

public sealed record RegisterStaffMemberCommand(string FullName, string Role, string? Phone) : IRequest<StaffMemberDto>;
