namespace MyCondo.Application.Features.Leasing.DTOs;

/// <summary>
/// Deliberately restricted view of an <see cref="Domain.Features.Leasing.OccupancyRegistrations.OccupancyRegistration"/>
/// for security staff — operational information only (name, photo, unit, occupancy/access status,
/// approved household/workers/vehicles). Never includes National ID, passport, permanent address,
/// email, or any identity-document/lease data — see <c>GetOccupancyRegistrationSecurityViewQueryHandler</c>.
/// </summary>
public sealed record OccupancyRegistrationSecurityViewDto(
    Guid OccupancyRegistrationId,
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    string BuildingName,
    string PrimaryFullName,
    Guid? PrimaryPhotoAttachmentId,
    string OccupancyType,
    string Status,
    bool AccessEligible,
    DateTimeOffset? ActivatedAtUtc,
    DateTimeOffset? MovedOutAtUtc,
    IReadOnlyList<SecurityHouseholdMemberDto> HouseholdMembers,
    IReadOnlyList<SecurityWorkerDto> Workers,
    IReadOnlyList<SecurityVehicleDto> Vehicles);

public sealed record SecurityHouseholdMemberDto(string FullName, string RelationshipToPrimary);

public sealed record SecurityWorkerDto(string FullName, string WorkerType, string VerificationStatus);

public sealed record SecurityVehicleDto(string RegistrationNumber, string VehicleType);
