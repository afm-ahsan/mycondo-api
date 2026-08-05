using MyCondo.Application.Features.Security.SebaVisits.DTOs;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.SebaVisits;

namespace MyCondo.Application.Features.Security.SebaVisits.Mappings;

internal static class SebaVisitMappings
{
    public static SebaVisitDto ToDto(this AccessSession session, SebaVisitDetail detail) => new(
        session.Id.Value, detail.VisitorFullName, detail.VisitorPhone, detail.Organization,
        detail.DepartmentOrEmployeeToMeet, detail.TokenNumber, detail.RelatedReferenceType,
        detail.RelatedReferenceId, detail.ServiceOutcome, detail.Acknowledged, session.EntryGateId.Value,
        session.EntryAtUtc, session.ExitGateId?.Value, session.ExitAtUtc, session.Status.ToString());
}
