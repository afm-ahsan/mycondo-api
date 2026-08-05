using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInDomesticWorker;

public sealed record CheckInDomesticWorkerCommand(
    Guid DomesticWorkerProfileId,
    Guid HostFlatId,
    Guid EntryGateId,
    string? Remarks,
    string? OverrideReason
) : IRequest<AccessSessionDto>;
