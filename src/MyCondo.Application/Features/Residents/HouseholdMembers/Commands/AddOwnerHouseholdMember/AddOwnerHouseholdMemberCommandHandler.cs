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

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Commands.AddOwnerHouseholdMember;

public sealed class AddOwnerHouseholdMemberCommandHandler(
    IResidentRepository residents,
    IFlatRepository flats,
    IResidentHouseholdMemberRepository members,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<AddOwnerHouseholdMemberCommandHandler> logger
) : IRequestHandler<AddOwnerHouseholdMemberCommand, ResidentHouseholdMemberDto>
{
    private const string OwnershipManagePermission = "ownership.manage";

    public async ValueTask<ResidentHouseholdMemberDto> Handle(
        AddOwnerHouseholdMemberCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentId residentId = new(command.ResidentId);
        Resident resident = await residents.GetByIdAsync(residentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), command.ResidentId);
        if (resident.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), command.ResidentId);
        }

        Flat flat = await flats.GetByIdAsync(resident.FlatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), resident.FlatId.Value);
        if (!currentUser.HasPermissionForBuilding(OwnershipManagePermission, flat.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage ownership for this Building.");
        }

        RelationshipType relationshipType = Enum.Parse<RelationshipType>(command.RelationshipType);

        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            tenantId, command.ResidentId, command.FullName, relationshipType, command.Gender, command.DateOfBirth,
            command.NationalIdNumber, command.BirthCertificateNumber, command.BloodGroup, command.Religion,
            command.Nationality, command.Occupation, clock.UtcNow);

        members.Add(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Owner household member {ResidentHouseholdMemberId} added for resident {ResidentId}, tenant {TenantId}",
            member.Id, command.ResidentId, tenantId);

        return member.ToDto();
    }
}
