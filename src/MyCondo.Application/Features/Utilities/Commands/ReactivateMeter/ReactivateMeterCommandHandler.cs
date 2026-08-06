using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Commands.ReactivateMeter;

public sealed class ReactivateMeterCommandHandler(
    IMeterRepository meters,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<ReactivateMeterCommandHandler> logger
) : IRequestHandler<ReactivateMeterCommand, MeterDto>
{
    public async ValueTask<MeterDto> Handle(ReactivateMeterCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        MeterId id = new(command.MeterId);
        Meter meter = await meters.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Meter), command.MeterId);
        if (meter.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Meter), command.MeterId);
        }

        meter.Reactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Meter {MeterId} reactivated, tenant {TenantId}", id, tenantId);

        return meter.ToDto();
    }
}
