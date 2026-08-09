using Mediator;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;

public sealed record CreateFlatOwnershipCommand(
    Guid UserId,
    Guid FlatId,
    DateOnly StartDate
) : IRequest<CreateFlatOwnershipResult>;

public sealed record CreateFlatOwnershipResult(Guid FlatOwnershipId, Guid UserId, Guid FlatId, DateOnly StartDate);
