namespace MyCondo.Application.Features.Payroll.StaffMembers.DTOs;

public sealed record StaffMemberDto(Guid StaffMemberId, string FullName, string Role, string? Phone, bool IsActive);
