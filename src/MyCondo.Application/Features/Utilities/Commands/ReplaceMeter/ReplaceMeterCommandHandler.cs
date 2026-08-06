using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Commands.ReplaceMeter;

/// <summary>
/// Replacing a meter carries its current flat assignment over to the new meter automatically — the
/// resident being billed doesn't change just because the physical device did, and leaving the new
/// meter unassigned would be an obvious operational gap the source spec's "meter replacement history"
/// requirement implies should not exist.
/// </summary>
public sealed class ReplaceMeterCommandHandler(
    IMeterRepository meters,
    IMeterAssignmentRepository assignments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ReplaceMeterCommandHandler> logger
) : IRequestHandler<ReplaceMeterCommand, ReplaceMeterResultDto>
{
    public async ValueTask<ReplaceMeterResultDto> Handle(ReplaceMeterCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        MeterId id = new(command.MeterId);
        Meter oldMeter = await meters.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Meter), command.MeterId);
        if (oldMeter.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Meter), command.MeterId);
        }

        string newMeterNumber = command.NewMeterNumber.Trim();
        Meter? clashing = await meters.GetByMeterNumberAsync(tenantId, oldMeter.UtilityType, newMeterNumber, cancellationToken);
        if (clashing is not null)
        {
            throw new ConflictException($"A {oldMeter.UtilityType} meter with number '{newMeterNumber}' already exists for this tenant.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        Meter newMeter = oldMeter.ReplaceWith(newMeterNumber, nowUtc);
        meters.Add(newMeter);

        MeterAssignment? openAssignment = await assignments.GetOpenForMeterAsync(tenantId, oldMeter.Id, cancellationToken);
        if (openAssignment is not null)
        {
            openAssignment.EndAssignment(nowUtc);
            MeterAssignment carriedOver = MeterAssignment.Assign(tenantId, newMeter.Id, openAssignment.FlatId, nowUtc);
            assignments.Add(carriedOver);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Meter {OldMeterId} replaced by {NewMeterId} ('{NewMeterNumber}'), tenant {TenantId}",
            oldMeter.Id, newMeter.Id, newMeterNumber, tenantId);

        return new ReplaceMeterResultDto(oldMeter.ToDto(), newMeter.ToDto());
    }
}
