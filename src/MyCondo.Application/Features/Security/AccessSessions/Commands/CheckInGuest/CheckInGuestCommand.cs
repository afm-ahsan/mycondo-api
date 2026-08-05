using Mediator;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInGuest;

public sealed record CheckInGuestCommand(
    Guid GuestProfileId,
    Guid HostFlatId,
    string? PurposeOfVisit,
    Guid EntryGateId,
    string? PassOrQrNumber,
    string? Remarks,
    string? OverrideReason
) : IRequest<AccessSessionDto>;
