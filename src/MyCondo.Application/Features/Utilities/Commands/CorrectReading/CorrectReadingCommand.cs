using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.CorrectReading;

public sealed record CorrectReadingCommand(
    Guid ReadingId,
    decimal PreviousReading,
    decimal PresentReading,
    DateOnly ReadingDate,
    string? OverrideReason,
    string Reason
) : IRequest<ReadingDto>;
