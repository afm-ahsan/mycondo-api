using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.CheckInPoolSession;

public sealed record CheckInPoolSessionCommand(
    Guid FacilityId,
    Guid FlatId,
    string PersonType,
    string AgeCategory,
    Guid? AccompaniedBySessionId,
    bool SafetyAcknowledged,
    string? OverrideReason
) : IRequest<PoolSessionDto>;
