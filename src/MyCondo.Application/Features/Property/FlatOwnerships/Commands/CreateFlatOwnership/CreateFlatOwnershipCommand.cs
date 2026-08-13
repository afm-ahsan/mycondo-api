using Mediator;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;

public sealed record CreateFlatOwnershipCommand(
    Guid ResidentId,
    Guid FlatId,
    DateOnly StartDate
) : IRequest<CreateFlatOwnershipResult>;

public sealed record CreateFlatOwnershipResult(Guid FlatOwnershipId, Guid ResidentId, Guid FlatId, DateOnly StartDate);
