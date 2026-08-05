using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutServiceProvider;

public sealed record CheckOutServiceProviderCommand(Guid AccessSessionId, Guid ExitGateId) : IRequest<AccessSessionDto>;
