using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;
using MyCondo.Application.Features.Residents.HouseholdMembers.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Commands.DeactivateOwnerHouseholdMember;

public sealed class DeactivateOwnerHouseholdMemberCommandHandler(
    IResidentHouseholdMemberRepository members,
    IResidentRepository residents,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateOwnerHouseholdMemberCommandHandler> logger
) : IRequestHandler<DeactivateOwnerHouseholdMemberCommand, ResidentHouseholdMemberDto>
{
    private const string OwnershipManagePermission = "ownership.manage";

    public async ValueTask<ResidentHouseholdMemberDto> Handle(
        DeactivateOwnerHouseholdMemberCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentHouseholdMemberId id = new(command.ResidentHouseholdMemberId);
        ResidentHouseholdMember member = await members.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ResidentHouseholdMember), command.ResidentHouseholdMemberId);
        if (member.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ResidentHouseholdMember), command.ResidentHouseholdMemberId);
        }

        Resident resident = await residents.GetByIdAsync(new ResidentId(member.ResidentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), member.ResidentId);
        Flat flat = await flats.GetByIdAsync(resident.FlatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), resident.FlatId.Value);
        if (!currentUser.HasPermissionForBuilding(OwnershipManagePermission, flat.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage ownership for this Building.");
        }

        member.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Owner household member {ResidentHouseholdMemberId} deactivated, tenant {TenantId}", id, tenantId);

        return member.ToDto();
    }
}
