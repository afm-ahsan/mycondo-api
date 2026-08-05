using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Domain.Features.Security.AccessSessions;

namespace MyCondo.Application.Features.Security.AccessSessions.Mappings;

internal static class AccessSessionMappings
{
    public static AccessSessionDto ToDto(this AccessSession session) => new(
        session.Id.Value,
        session.AccessCategory.ToString(),
        session.GuestProfileId?.Value,
        session.VehicleId?.Value,
        session.HostFlatId?.Value,
        session.PurposeOfVisit,
        session.EntryGateId.Value,
        session.EntryAtUtc,
        session.ExitGateId?.Value,
        session.ExitAtUtc,
        session.CheckedInBy,
        session.CheckedOutBy,
        session.ApprovalStatus.ToString(),
        session.PassOrQrNumber,
        session.Remarks,
        session.Status.ToString(),
        session.OverrideReason);
}
