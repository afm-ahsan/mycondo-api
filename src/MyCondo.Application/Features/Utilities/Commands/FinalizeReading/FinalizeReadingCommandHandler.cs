using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Commands.FinalizeReading;

/// <summary>
/// Abnormal-consumption threshold is a documented placeholder, not a tuned business rule (no
/// threshold was specified anywhere in the source spec) — flagged if consumption exceeds 2x the
/// meter's average of its last 3 finalized readings; never flagged with fewer than 3 prior readings
/// (insufficient history).
/// </summary>
public sealed class FinalizeReadingCommandHandler(
    IReadingRepository readings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<FinalizeReadingCommandHandler> logger
) : IRequestHandler<FinalizeReadingCommand, ReadingDto>
{
    private const int HistoryWindow = 3;
    private const decimal AbnormalMultiplier = 2m;

    public async ValueTask<ReadingDto> Handle(FinalizeReadingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ReadingId id = new(command.ReadingId);
        Reading reading = await readings.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Reading), command.ReadingId);
        if (reading.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Reading), command.ReadingId);
        }

        IReadOnlyList<Reading> recent = await readings.GetRecentFinalizedAsync(
            tenantId, reading.MeterId, HistoryWindow, cancellationToken);

        bool isAbnormal = false;
        string? abnormalReason = null;
        if (recent.Count == HistoryWindow)
        {
            decimal average = recent.Average(r => r.ConsumptionUnits);
            if (average > 0 && reading.ConsumptionUnits > average * AbnormalMultiplier)
            {
                isAbnormal = true;
                abnormalReason = $"Consumption {reading.ConsumptionUnits:0.###} exceeds {AbnormalMultiplier}x the last {HistoryWindow}-reading average ({average:0.###}).";
            }
        }

        reading.Finalize(currentUser.UserId, isAbnormal, abnormalReason, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reading {ReadingId} finalized (abnormal={IsAbnormal}), tenant {TenantId}", id, isAbnormal, tenantId);

        return reading.ToDto();
    }
}
