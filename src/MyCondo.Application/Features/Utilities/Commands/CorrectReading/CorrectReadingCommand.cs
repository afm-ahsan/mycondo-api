using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.CorrectReading;

/// <summary>Resource-derived-feature write pilot for the ADR-032 Task 10 lifecycle read/write
/// classification — proves a Task 09A resource-derived-entitlement write is blocked under a
/// Restricted/Expired subscription, and that lifecycle enforcement runs before that resolution.</summary>
public sealed record CorrectReadingCommand(
    Guid ReadingId,
    decimal PreviousReading,
    decimal PresentReading,
    DateOnly ReadingDate,
    string? OverrideReason,
    string Reason
) : IRequest<ReadingDto>, IHasReadingId, IRequiresResolvedFeature, ILifecycleWriteOperation;
