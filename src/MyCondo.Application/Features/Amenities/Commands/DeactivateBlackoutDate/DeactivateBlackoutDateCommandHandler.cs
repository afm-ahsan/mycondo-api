using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.BlackoutDates;

namespace MyCondo.Application.Features.Amenities.Commands.DeactivateBlackoutDate;

public sealed class DeactivateBlackoutDateCommandHandler(
    IBlackoutDateRepository blackoutDates,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateBlackoutDateCommandHandler> logger
) : IRequestHandler<DeactivateBlackoutDateCommand, BlackoutDateDto>
{
    public async ValueTask<BlackoutDateDto> Handle(DeactivateBlackoutDateCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BlackoutDateId id = new(command.BlackoutDateId);
        BlackoutDate blackoutDate = await blackoutDates.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(BlackoutDate), command.BlackoutDateId);
        if (blackoutDate.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(BlackoutDate), command.BlackoutDateId);
        }

        blackoutDate.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Blackout date {BlackoutDateId} deactivated, tenant {TenantId}", id, tenantId);

        return blackoutDate.ToDto();
    }
}
