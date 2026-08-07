using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;

namespace MyCondo.Application.Features.Leasing.Commands.DeactivateHouseholdMember;

public sealed class DeactivateHouseholdMemberCommandHandler(
    IHouseholdMemberRepository members,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateHouseholdMemberCommandHandler> logger
) : IRequestHandler<DeactivateHouseholdMemberCommand, HouseholdMemberDto>
{
    public async ValueTask<HouseholdMemberDto> Handle(DeactivateHouseholdMemberCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        HouseholdMemberId id = new(command.HouseholdMemberId);
        HouseholdMember member = await members.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdMember), command.HouseholdMemberId);
        if (member.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(HouseholdMember), command.HouseholdMemberId);
        }

        member.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Household member {HouseholdMemberId} deactivated, tenant {TenantId}", id, tenantId);

        return member.ToDto();
    }
}
