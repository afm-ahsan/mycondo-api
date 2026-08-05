using Mediator;
using MyCondo.Application.Features.Security.SebaVisits.DTOs;

namespace MyCondo.Application.Features.Security.SebaVisits.Commands.CheckOutSebaVisitor;

public sealed record CheckOutSebaVisitorCommand(
    Guid AccessSessionId,
    Guid ExitGateId,
    string? ServiceOutcome,
    bool Acknowledged
) : IRequest<SebaVisitDto>;
