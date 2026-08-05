using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.AccessSessions;

namespace MyCondo.Domain.Features.Security.SebaVisits;

/// <summary>
/// Seba-office-visit-specific fields, kept off the shared <see cref="AccessSession"/> table as a 1:1
/// sidecar rather than bloating every category with rarely-used columns — Seba visitors are mostly
/// one-off public callers rather than recurring parties, so (unlike Guest/Vehicle) there is no
/// standing profile to reuse; this record is the per-visit snapshot the spec explicitly allows for
/// registers with no reusable identity. <see cref="RelatedReferenceType"/>/<see cref="RelatedReferenceId"/>
/// are a deliberately loose, nullable reference — Complaints/ServiceRequests/Payments/Documents/
/// Enquiries/Appointments don't exist as modules yet, so this avoids coupling to them prematurely.
/// </summary>
public sealed class SebaVisitDetail : AggregateRoot<SebaVisitDetailId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public AccessSessionId AccessSessionId { get; private set; }
    public string VisitorFullName { get; private set; }
    public string? VisitorPhone { get; private set; }
    public string? Organization { get; private set; }
    public string? DepartmentOrEmployeeToMeet { get; private set; }
    public string? TokenNumber { get; private set; }
    public string? RelatedReferenceType { get; private set; }
    public Guid? RelatedReferenceId { get; private set; }
    public string? ServiceOutcome { get; private set; }
    public bool Acknowledged { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private SebaVisitDetail()
    {
        VisitorFullName = null!;
    }

    private SebaVisitDetail(
        SebaVisitDetailId id,
        Guid tenantId,
        AccessSessionId accessSessionId,
        string visitorFullName,
        string? visitorPhone,
        string? organization,
        string? departmentOrEmployeeToMeet,
        string? tokenNumber,
        string? relatedReferenceType,
        Guid? relatedReferenceId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        AccessSessionId = accessSessionId;
        VisitorFullName = visitorFullName;
        VisitorPhone = visitorPhone;
        Organization = organization;
        DepartmentOrEmployeeToMeet = departmentOrEmployeeToMeet;
        TokenNumber = tokenNumber;
        RelatedReferenceType = relatedReferenceType;
        RelatedReferenceId = relatedReferenceId;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static SebaVisitDetail Record(
        Guid tenantId,
        AccessSessionId accessSessionId,
        string visitorFullName,
        string? visitorPhone,
        string? organization,
        string? departmentOrEmployeeToMeet,
        string? tokenNumber,
        string? relatedReferenceType,
        Guid? relatedReferenceId,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visitorFullName);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new SebaVisitDetail(
            SebaVisitDetailId.New(), tenantId, accessSessionId, visitorFullName.Trim(), visitorPhone?.Trim(),
            organization?.Trim(), departmentOrEmployeeToMeet?.Trim(), tokenNumber?.Trim(), relatedReferenceType?.Trim(),
            relatedReferenceId, nowUtc);
    }

    public void RecordOutcome(string? serviceOutcome, bool acknowledged)
    {
        ServiceOutcome = serviceOutcome?.Trim();
        Acknowledged = acknowledged;
        Version++;
    }
}
