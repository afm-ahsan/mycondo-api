using Mediator;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.EndFlatOwnership;

public sealed record EndFlatOwnershipCommand(
    Guid FlatOwnershipId,
    DateOnly EndDate
) : IRequest;
