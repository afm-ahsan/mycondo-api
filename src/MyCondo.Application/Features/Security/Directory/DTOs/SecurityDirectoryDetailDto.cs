namespace MyCondo.Application.Features.Security.Directory.DTOs;

/// <summary>
/// Deliberately restricted detail view of a merged security-directory entry (Owner via
/// <c>FlatOwnership</c>, or Tenant via <c>OccupancyRegistration</c>) — operational information only
/// (name, phone, photo, unit, resident type, access/occupancy status). Never includes National ID,
/// passport, permanent address, email, religion, nationality, parents' names, or any other identity-
/// document/financial data — see <c>GetSecurityDirectoryDetailQueryHandler</c>.
///
/// <see cref="HouseholdMembers"/>, <see cref="Workers"/>, <see cref="Vehicles"/>, and
/// <see cref="ExtendedDetail"/> are each <c>null</c> when the caller lacks the matching granular
/// permission (<c>security.directory.household.view</c> / <c>.worker.view</c> / <c>.vehicle.view</c> /
/// <c>.detail.view</c>) — distinct from an empty list, which means the caller is authorized but there is
/// nothing on file. The API never returns a section the caller isn't authorized for.
/// </summary>
public sealed record SecurityDirectoryDetailDto(
    Guid EntryId,
    string ResidentType,
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    string BuildingName,
    string PrimaryFullName,
    string? PrimaryPhone,
    Guid? PrimaryPhotoAttachmentId,
    string AccessStatus,
    string OccupancyStatus,
    IReadOnlyList<SecurityDirectoryHouseholdMemberDto>? HouseholdMembers,
    IReadOnlyList<SecurityDirectoryWorkerDto>? Workers,
    IReadOnlyList<SecurityDirectoryVehicleDto>? Vehicles,
    SecurityDirectoryExtendedDetailDto? ExtendedDetail);

public sealed record SecurityDirectoryHouseholdMemberDto(string FullName, string RelationshipToPrimary);

public sealed record SecurityDirectoryWorkerDto(string FullName, string WorkerType, string VerificationStatus);

public sealed record SecurityDirectoryVehicleDto(string RegistrationNumber, string VehicleType);

/// <summary>Extended occupancy/ownership timeline — activation/move-out for a Tenant entry,
/// start/end date for an Owner entry — gated by <c>security.directory.detail.view</c>.</summary>
public sealed record SecurityDirectoryExtendedDetailDto(
    DateTimeOffset? ActivatedAtUtc,
    DateTimeOffset? MovedOutAtUtc,
    DateOnly? OwnershipStartDate,
    DateOnly? OwnershipEndDate);
