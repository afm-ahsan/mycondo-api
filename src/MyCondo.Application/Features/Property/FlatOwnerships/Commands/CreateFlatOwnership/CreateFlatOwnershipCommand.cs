using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;

public sealed record CreateFlatOwnershipCommand(
    Guid ResidentId,
    Guid FlatId,
    DateOnly StartDate
) : IRequest<CreateFlatOwnershipResult>, ILifecycleWriteOperation;

public sealed record CreateFlatOwnershipResult(Guid FlatOwnershipId, Guid ResidentId, Guid FlatId, DateOnly StartDate);
