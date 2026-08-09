using Mediator;

namespace MyCondo.Application.Features.Me.Queries.GetMyFlats;

public sealed record GetMyFlatsQuery : IRequest<List<MyFlatDto>>;

public sealed record MyFlatDto(
    Guid FlatId,
    string FlatNumber,
    Guid BuildingId,
    string BuildingName,
    string RelationshipType,
    DateOnly? StartDate,
    DateOnly? EndDate);
