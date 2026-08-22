using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Directory.DTOs;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectoryDetail;

/// <summary>Builds the restricted security-facing detail view for either an Owner (<see cref="FlatOwnership"/>)
/// or Tenant (<see cref="OccupancyRegistration"/>) entry — see <see cref="SecurityDirectoryDetailDto"/>'s
/// doc comment for exactly what is and is not included, and how granular section permissions gate each
/// list. The endpoint filter only enforces the base <c>security.directory.view</c>; this handler checks
/// the four granular permissions itself before populating each optional section.</summary>
public sealed class GetSecurityDirectoryDetailQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IFlatOwnershipRepository flatOwnerships,
    IResidentRepository residents,
    IFlatRepository flats,
    IBuildingRepository buildings,
    IHouseholdMemberRepository householdMembers,
    IResidentHouseholdMemberRepository residentHouseholdMembers,
    IOccupancyRegistrationWorkerAssignmentRepository workerAssignments,
    IDomesticWorkerProfileRepository workers,
    IOccupancyRegistrationVehicleAssignmentRepository vehicleAssignments,
    IVehicleRepository vehicles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetSecurityDirectoryDetailQuery, SecurityDirectoryDetailDto>
{
    public async ValueTask<SecurityDirectoryDetailDto> Handle(
        GetSecurityDirectoryDetailQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        return query.ResidentType == "Owner"
            ? await BuildOwnerDetailAsync(tenantId, query.EntryId, cancellationToken)
            : await BuildTenantDetailAsync(tenantId, query.EntryId, cancellationToken);
    }

    private async ValueTask<SecurityDirectoryDetailDto> BuildTenantDetailAsync(
        Guid tenantId, Guid entryId, CancellationToken cancellationToken)
    {
        OccupancyRegistrationId registrationId = new(entryId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), entryId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), entryId);
        }

        Flat? flat = await flats.GetByIdAsync(registration.FlatId, cancellationToken);
        Building? building = flat is not null ? await buildings.GetByIdAsync(flat.BuildingId, cancellationToken) : null;
        bool authorized = registration.Status == OccupancyRegistrationStatus.Active;

        IReadOnlyList<SecurityDirectoryHouseholdMemberDto>? householdDtos = null;
        if (currentUser.HasPermission("security.directory.household.view"))
        {
            IReadOnlyList<HouseholdMember> members =
                await householdMembers.GetForRegistrationAsync(registrationId, cancellationToken);
            householdDtos = members.Where(m => m.IsActive)
                .Select(m => new SecurityDirectoryHouseholdMemberDto(m.FullName, m.RelationshipToPrimary))
                .ToList();
        }

        IReadOnlyList<SecurityDirectoryWorkerDto>? workerDtos = null;
        if (currentUser.HasPermission("security.directory.worker.view"))
        {
            IReadOnlyList<OccupancyRegistrationWorkerAssignment> workerLinks =
                await workerAssignments.GetForRegistrationAsync(registrationId, cancellationToken);
            List<SecurityDirectoryWorkerDto> list = [];
            foreach (OccupancyRegistrationWorkerAssignment link in workerLinks.Where(l => l.IsActive))
            {
                DomesticWorkerProfile? worker = await workers.GetByIdAsync(link.DomesticWorkerProfileId, cancellationToken);
                if (worker is not null)
                {
                    list.Add(new SecurityDirectoryWorkerDto(worker.FullName, worker.WorkerType.ToString(), worker.VerificationStatus.ToString()));
                }
            }

            workerDtos = list;
        }

        IReadOnlyList<SecurityDirectoryVehicleDto>? vehicleDtos = null;
        if (currentUser.HasPermission("security.directory.vehicle.view"))
        {
            IReadOnlyList<OccupancyRegistrationVehicleAssignment> vehicleLinks =
                await vehicleAssignments.GetForRegistrationAsync(registrationId, cancellationToken);
            List<SecurityDirectoryVehicleDto> list = [];
            foreach (OccupancyRegistrationVehicleAssignment link in vehicleLinks.Where(l => l.IsActive))
            {
                Vehicle? vehicle = await vehicles.GetByIdAsync(link.VehicleId, cancellationToken);
                if (vehicle is not null)
                {
                    list.Add(new SecurityDirectoryVehicleDto(vehicle.RegistrationNumber, vehicle.VehicleType.ToString()));
                }
            }

            vehicleDtos = list;
        }

        SecurityDirectoryExtendedDetailDto? extendedDetail = currentUser.HasPermission("security.directory.detail.view")
            ? new SecurityDirectoryExtendedDetailDto(registration.ActivatedAtUtc, registration.MovedOutAtUtc, null, null)
            : null;

        return new SecurityDirectoryDetailDto(
            registration.Id.Value, "Tenant", flat?.Id.Value ?? registration.FlatId.Value, flat?.FlatNumber ?? "—",
            flat?.BuildingId.Value ?? Guid.Empty, building?.Name ?? "—", registration.PrimaryFullName,
            registration.PrimaryPhone, registration.PrimaryPhotoAttachmentId, authorized ? "Authorized" : "Revoked",
            registration.Status.ToString(), householdDtos, workerDtos, vehicleDtos, extendedDetail);
    }

    private async ValueTask<SecurityDirectoryDetailDto> BuildOwnerDetailAsync(
        Guid tenantId, Guid entryId, CancellationToken cancellationToken)
    {
        FlatOwnershipId ownershipId = new(entryId);
        FlatOwnership ownership = await flatOwnerships.GetByIdAsync(ownershipId, cancellationToken)
            ?? throw new NotFoundException(nameof(FlatOwnership), entryId);
        if (ownership.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FlatOwnership), entryId);
        }

        Resident? owner = await residents.GetByIdAsync(new ResidentId(ownership.ResidentId), cancellationToken);
        Flat? flat = await flats.GetByIdAsync(ownership.FlatId, cancellationToken);
        Building? building = flat is not null ? await buildings.GetByIdAsync(flat.BuildingId, cancellationToken) : null;
        bool authorized = ownership.Status == FlatOwnershipStatus.Active;

        // No worker/vehicle assignment model exists against FlatOwnership/Resident today (only against
        // OccupancyRegistration) — an authorized caller still sees an empty list, not a missing section,
        // since they hold the permission and there is genuinely nothing to show.
        IReadOnlyList<SecurityDirectoryHouseholdMemberDto>? householdDtos = null;
        if (currentUser.HasPermission("security.directory.household.view"))
        {
            IReadOnlyList<ResidentHouseholdMember> members =
                await residentHouseholdMembers.GetForResidentAsync(ownership.ResidentId, cancellationToken);
            householdDtos = members.Where(m => m.IsActive)
                .Select(m => new SecurityDirectoryHouseholdMemberDto(m.FullName, m.RelationshipType.ToString()))
                .ToList();
        }

        IReadOnlyList<SecurityDirectoryWorkerDto>? workerDtos =
            currentUser.HasPermission("security.directory.worker.view") ? [] : null;
        IReadOnlyList<SecurityDirectoryVehicleDto>? vehicleDtos =
            currentUser.HasPermission("security.directory.vehicle.view") ? [] : null;

        SecurityDirectoryExtendedDetailDto? extendedDetail = currentUser.HasPermission("security.directory.detail.view")
            ? new SecurityDirectoryExtendedDetailDto(null, null, ownership.StartDate, ownership.EndDate)
            : null;

        return new SecurityDirectoryDetailDto(
            ownership.Id.Value, "Owner", flat?.Id.Value ?? ownership.FlatId.Value, flat?.FlatNumber ?? "—",
            flat?.BuildingId.Value ?? Guid.Empty, building?.Name ?? "—", owner?.FullName ?? "—", owner?.Phone,
            null, authorized ? "Authorized" : "Revoked", ownership.Status.ToString(), householdDtos, workerDtos,
            vehicleDtos, extendedDetail);
    }
}
