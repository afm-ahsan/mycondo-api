using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Commands.MarkMeterFaulty;

public sealed class MarkMeterFaultyCommandHandler(
    IMeterRepository meters,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<MarkMeterFaultyCommandHandler> logger
) : IRequestHandler<MarkMeterFaultyCommand, MeterDto>
{
    public async ValueTask<MeterDto> Handle(MarkMeterFaultyCommand command, CancellationToken cancellationToken)
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

        meter.MarkFaulty(command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Meter {MeterId} marked faulty, tenant {TenantId}", id, tenantId);

        return meter.ToDto();
    }
}
