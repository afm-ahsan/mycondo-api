using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Commands.RecordReading;

public sealed class RecordReadingCommandHandler(
    IMeterRepository meters,
    IMeterAssignmentRepository assignments,
    IReadingRepository readings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordReadingCommandHandler> logger
) : IRequestHandler<RecordReadingCommand, ReadingDto>
{
    public async ValueTask<ReadingDto> Handle(RecordReadingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        MeterId meterId = new(command.MeterId);
        Meter meter = await meters.GetByIdAsync(meterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Meter), command.MeterId);
        if (meter.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Meter), command.MeterId);
        }

        MeterAssignment currentAssignment = await assignments.GetOpenForMeterAsync(tenantId, meterId, cancellationToken)
            ?? throw new ConflictException($"Meter {command.MeterId} has no current flat assignment; assign it before recording a reading.");

        bool alreadyActive = await readings.ExistsActiveForMeterAndPeriodAsync(
            tenantId, meterId, command.PeriodStart, command.PeriodEnd, cancellationToken);
        if (alreadyActive)
        {
            throw new ConflictException(
                $"A reading already exists for meter {command.MeterId} covering {command.PeriodStart}..{command.PeriodEnd}.");
        }

        Reading? lastFinalized = await readings.GetLastFinalizedAsync(tenantId, meterId, cancellationToken);
        if (lastFinalized is not null && lastFinalized.PresentReading != command.PreviousReading
            && string.IsNullOrWhiteSpace(command.OverrideReason))
        {
            throw new ConflictException(
                $"PreviousReading ({command.PreviousReading}) does not match meter {command.MeterId}'s last finalized reading ({lastFinalized.PresentReading}); provide OverrideReason to proceed.");
        }

        Reading reading = Reading.Record(
            tenantId, meterId, currentAssignment.FlatId, meter.UtilityType, meter.BuildingId, command.PeriodStart,
            command.PeriodEnd, command.PreviousReading, command.PresentReading, command.ReadingDate,
            command.OverrideReason, null, clock.UtcNow);

        readings.Add(reading);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reading {ReadingId} recorded for meter {MeterId}, period {PeriodStart}..{PeriodEnd}, tenant {TenantId}",
            reading.Id, meterId, command.PeriodStart, command.PeriodEnd, tenantId);

        return reading.ToDto();
    }
}
