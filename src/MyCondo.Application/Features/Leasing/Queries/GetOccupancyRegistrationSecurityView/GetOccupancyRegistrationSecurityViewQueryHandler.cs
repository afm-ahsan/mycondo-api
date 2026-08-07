using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationSecurityView;

/// <summary>Builds the restricted security-facing view — see <see cref="OccupancyRegistrationSecurityViewDto"/>'s
/// doc comment for exactly what is and is not included.</summary>
public sealed class GetOccupancyRegistrationSecurityViewQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IFlatRepository flats,
    IBuildingRepository buildings,
    IHouseholdMemberRepository householdMembers,
    IOccupancyRegistrationWorkerAssignmentRepository workerAssignments,
    IDomesticWorkerProfileRepository workers,
    IOccupancyRegistrationVehicleAssignmentRepository vehicleAssignments,
    IVehicleRepository vehicles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetOccupancyRegistrationSecurityViewQuery, OccupancyRegistrationSecurityViewDto>
{
    public async ValueTask<OccupancyRegistrationSecurityViewDto> Handle(
        GetOccupancyRegistrationSecurityViewQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId registrationId = new(query.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), query.OccupancyRegistrationId);
        }

        Flat? flat = await flats.GetByIdAsync(registration.FlatId, cancellationToken);
        Building? building = flat is not null ? await buildings.GetByIdAsync(flat.BuildingId, cancellationToken) : null;

        IReadOnlyList<HouseholdMember> members = await householdMembers.GetForRegistrationAsync(registrationId, cancellationToken);
        List<SecurityHouseholdMemberDto> memberDtos = members
            .Where(m => m.IsActive)
            .Select(m => new SecurityHouseholdMemberDto(m.FullName, m.RelationshipToPrimary))
            .ToList();

        IReadOnlyList<OccupancyRegistrationWorkerAssignment> workerLinks =
            await workerAssignments.GetForRegistrationAsync(registrationId, cancellationToken);
        List<SecurityWorkerDto> workerDtos = [];
        foreach (OccupancyRegistrationWorkerAssignment link in workerLinks.Where(l => l.IsActive))
        {
            DomesticWorkerProfile? worker = await workers.GetByIdAsync(link.DomesticWorkerProfileId, cancellationToken);
            if (worker is not null)
            {
                workerDtos.Add(new SecurityWorkerDto(worker.FullName, worker.WorkerType.ToString(), worker.VerificationStatus.ToString()));
            }
        }

        IReadOnlyList<OccupancyRegistrationVehicleAssignment> vehicleLinks =
            await vehicleAssignments.GetForRegistrationAsync(registrationId, cancellationToken);
        List<SecurityVehicleDto> vehicleDtos = [];
        foreach (OccupancyRegistrationVehicleAssignment link in vehicleLinks.Where(l => l.IsActive))
        {
            Vehicle? vehicle = await vehicles.GetByIdAsync(link.VehicleId, cancellationToken);
            if (vehicle is not null)
            {
                vehicleDtos.Add(new SecurityVehicleDto(vehicle.RegistrationNumber, vehicle.VehicleType.ToString()));
            }
        }

        return new OccupancyRegistrationSecurityViewDto(
            registration.Id.Value, registration.FlatId.Value, flat?.FlatNumber ?? "—",
            flat?.BuildingId.Value ?? Guid.Empty, building?.Name ?? "—", registration.PrimaryFullName,
            registration.PrimaryPhotoAttachmentId, registration.OccupancyType.ToString(), registration.Status.ToString(),
            registration.Status == OccupancyRegistrationStatus.Active, registration.ActivatedAtUtc,
            registration.MovedOutAtUtc, memberDtos, workerDtos, vehicleDtos);
    }
}
