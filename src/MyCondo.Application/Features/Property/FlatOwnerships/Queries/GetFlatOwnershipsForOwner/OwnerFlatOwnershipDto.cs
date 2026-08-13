namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForOwner;

public sealed record OwnerFlatOwnershipDto(
    Guid FlatOwnershipId,
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    string BuildingName,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate
);
