using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInServiceProvider;

public sealed record CheckInServiceProviderCommand(
    Guid ServiceProviderProfileId,
    Guid HostFlatId,
    Guid EntryGateId,
    string? Remarks,
    string? OverrideReason
) : IRequest<AccessSessionDto>;
