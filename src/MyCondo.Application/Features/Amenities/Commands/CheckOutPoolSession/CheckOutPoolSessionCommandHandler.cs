using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.PoolSessions;

namespace MyCondo.Application.Features.Amenities.Commands.CheckOutPoolSession;

public sealed class CheckOutPoolSessionCommandHandler(
    IPoolSessionRepository poolSessions,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckOutPoolSessionCommandHandler> logger
) : IRequestHandler<CheckOutPoolSessionCommand, PoolSessionDto>
{
    public async ValueTask<PoolSessionDto> Handle(CheckOutPoolSessionCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PoolSessionId id = new(command.PoolSessionId);
        PoolSession session = await poolSessions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(PoolSession), command.PoolSessionId);
        if (session.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(PoolSession), command.PoolSessionId);
        }

        session.CheckOut(currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Pool session {PoolSessionId} checked out, tenant {TenantId}", id, tenantId);

        return session.ToDto();
    }
}
