using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;

public sealed class CreateFlatOwnershipCommandHandler(
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateFlatOwnershipCommandHandler> logger
) : IRequestHandler<CreateFlatOwnershipCommand, CreateFlatOwnershipResult>
{
    public async ValueTask<CreateFlatOwnershipResult> Handle(CreateFlatOwnershipCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);

        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        UserId userId = new(command.UserId);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), command.UserId);

        if (user.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(User), command.UserId);
        }

        bool alreadyActive = await flatOwnerships.ExistsActiveForUserAndFlatAsync(
            tenantId, command.UserId, flatId, cancellationToken);
        if (alreadyActive)
        {
            throw new ConflictException("This user already has an active ownership relationship with this flat.");
        }

        FlatOwnership ownership = FlatOwnership.Grant(tenantId, command.UserId, flatId, command.StartDate, clock.UtcNow);

        flatOwnerships.Add(ownership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FlatOwnership {FlatOwnershipId} granted: user {UserId} owns flat {FlatId} for tenant {TenantId}",
            ownership.Id, command.UserId, command.FlatId, tenantId);

        return new CreateFlatOwnershipResult(ownership.Id.Value, command.UserId, command.FlatId, command.StartDate);
    }
}
