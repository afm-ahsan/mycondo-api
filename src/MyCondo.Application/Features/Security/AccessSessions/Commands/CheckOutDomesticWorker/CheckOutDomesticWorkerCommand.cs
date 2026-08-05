using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutDomesticWorker;

public sealed record CheckOutDomesticWorkerCommand(Guid AccessSessionId, Guid ExitGateId) : IRequest<AccessSessionDto>;
