using Mediator;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForFlat;

public sealed record GetFlatOwnershipsForFlatQuery(Guid FlatId) : IRequest<List<FlatOwnershipDto>>;

public sealed record FlatOwnershipDto(
    Guid FlatOwnershipId,
    Guid ResidentId,
    string OwnerFullName,
    Guid FlatId,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate);
