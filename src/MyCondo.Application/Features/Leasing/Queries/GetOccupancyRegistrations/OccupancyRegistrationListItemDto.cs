namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrations;

/// <summary>List-page projection for Tenant Registrations — adds the human-readable Flat/Building
/// context (absent from the base <c>OccupancyRegistrationDto</c>, which only carries the raw
/// <c>FlatId</c>) so the register doesn't have to expose GUIDs, mirroring
/// <c>FlatOwnerRegisterDto</c>'s list-projection pattern.</summary>
public sealed record OccupancyRegistrationListItemDto(
    Guid OccupancyRegistrationId,
    string PrimaryFullName,
    string? PrimaryEmail,
    string? PrimaryPhone,
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    string BuildingName,
    string OccupancyType,
    string Status,
    DateOnly? MoveInExpectedDate
);
