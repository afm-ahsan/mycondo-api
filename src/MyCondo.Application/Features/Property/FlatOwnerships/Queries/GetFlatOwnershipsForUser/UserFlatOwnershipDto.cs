namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForUser;

public sealed record UserFlatOwnershipDto(
    Guid FlatOwnershipId,
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    string BuildingName,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate
);
