namespace MyCondo.Application.Features.Leasing.DTOs;

/// <summary>List-row shape for the security-facing directory — see
/// <see cref="OccupancyRegistrationSecurityViewDto"/> for the full detail shape and masking rules.</summary>
public sealed record OccupancyRegistrationSecuritySummaryDto(
    Guid OccupancyRegistrationId,
    string FlatNumber,
    string BuildingName,
    string PrimaryFullName,
    Guid? PrimaryPhotoAttachmentId,
    string Status,
    bool AccessEligible);
