using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payroll.StaffMembers.DTOs;
using MyCondo.Application.Features.Payroll.StaffMembers.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Commands.RegisterStaffMember;

public sealed class RegisterStaffMemberCommandHandler(
    IStaffMemberRepository staffMembers,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RegisterStaffMemberCommandHandler> logger
) : IRequestHandler<RegisterStaffMemberCommand, StaffMemberDto>
{
    public async ValueTask<StaffMemberDto> Handle(RegisterStaffMemberCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        StaffRole role = Enum.Parse<StaffRole>(command.Role);

        StaffMember staffMember = StaffMember.Register(tenantId, command.FullName, role, command.Phone, clock.UtcNow);

        staffMembers.Add(staffMember);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Staff member {StaffMemberId} '{FullName}' registered for tenant {TenantId}",
            staffMember.Id, staffMember.FullName, tenantId);

        return staffMember.ToDto();
    }
}
