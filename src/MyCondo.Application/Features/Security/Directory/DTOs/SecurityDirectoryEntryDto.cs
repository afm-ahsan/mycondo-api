namespace MyCondo.Application.Features.Security.Directory.DTOs;

/// <summary>
/// List-row shape for the merged, security-facing resident directory — one row per active
/// <c>OccupancyRegistration</c> (Tenant) or active <c>FlatOwnership</c> (Owner). <see cref="EntryId"/> is
/// the underlying aggregate's id (the <c>OccupancyRegistrationId</c> for a Tenant row, the
/// <c>FlatOwnershipId</c> for an Owner row) — callers must pass both <see cref="EntryId"/> and
/// <see cref="ResidentType"/> back to the detail endpoint to resolve the right aggregate. See
/// <see cref="SecurityDirectoryDetailDto"/> for the full detail shape and masking rules.
/// </summary>
public sealed record SecurityDirectoryEntryDto(
    Guid EntryId,
    string ResidentType,
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    string BuildingName,
    string PrimaryFullName,
    Guid? PrimaryPhotoAttachmentId,
    string AccessStatus,
    string OccupancyStatus);
