using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.RecordReading;

public sealed record RecordReadingCommand(
    Guid MeterId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal PreviousReading,
    decimal PresentReading,
    DateOnly ReadingDate,
    string? OverrideReason
) : IRequest<ReadingDto>;
