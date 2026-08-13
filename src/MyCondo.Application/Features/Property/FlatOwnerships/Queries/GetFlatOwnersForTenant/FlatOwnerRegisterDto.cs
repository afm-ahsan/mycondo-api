namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnersForTenant;

public sealed record FlatOwnerRegisterDto(
    Guid FlatOwnershipId,
    Guid ResidentId,
    string OwnerFullName,
    string? OwnerEmail,
    string? OwnerPhone,
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    string BuildingName,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate
);
