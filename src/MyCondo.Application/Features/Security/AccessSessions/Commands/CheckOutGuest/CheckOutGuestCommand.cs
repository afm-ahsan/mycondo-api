using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutGuest;

public sealed record CheckOutGuestCommand(Guid AccessSessionId, Guid ExitGateId) : IRequest<AccessSessionDto>;
