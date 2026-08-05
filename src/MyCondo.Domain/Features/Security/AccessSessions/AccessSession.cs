using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions.Exceptions;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Guests;
using MyCondo.Domain.Features.Security.ServiceProviders;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Domain.Features.Security.AccessSessions;

/// <summary>
/// Shared entry/exit record for every access-and-movement register (guest, vehicle, domestic worker,
/// service provider, seba visitor — everything except staff, which uses its own shift-aware
/// <c>AttendanceRecord</c> instead, see that type's doc comment), so "who's currently inside" and
/// "unclosed visits" queries work uniformly across categories instead of needing a UNION across
/// separate tables. At most one of the profile-id properties is populated, matching
/// <see cref="AccessCategory"/> — enforced by each factory method, not by a runtime check against
/// every possible combination. SebaVisitor populates none of them; its identity fields live entirely
/// in the <c>SebaVisitDetail</c> sidecar (see that type's doc comment for why).
/// </summary>
public sealed class AccessSession : AggregateRoot<AccessSessionId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public AccessCategory AccessCategory { get; private set; }
    public GuestProfileId? GuestProfileId { get; private set; }
    public VehicleId? VehicleId { get; private set; }
    public DomesticWorkerProfileId? DomesticWorkerProfileId { get; private set; }
    public ServiceProviderProfileId? ServiceProviderProfileId { get; private set; }
    public FlatId? HostFlatId { get; private set; }
    public string? PurposeOfVisit { get; private set; }
    public GateId EntryGateId { get; private set; }
    public DateTimeOffset EntryAtUtc { get; private set; }
    public GateId? ExitGateId { get; private set; }
    public DateTimeOffset? ExitAtUtc { get; private set; }
    public Guid? CheckedInBy { get; private set; }
    public Guid? CheckedOutBy { get; private set; }
    public AccessApprovalStatus ApprovalStatus { get; private set; }
    public string? PassOrQrNumber { get; private set; }
    public string? Remarks { get; private set; }
    public AccessSessionStatus Status { get; private set; }
    public string? OverrideReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private AccessSession() { }

    private AccessSession(
        AccessSessionId id,
        Guid tenantId,
        AccessCategory accessCategory,
        GuestProfileId? guestProfileId,
        VehicleId? vehicleId,
        DomesticWorkerProfileId? domesticWorkerProfileId,
        ServiceProviderProfileId? serviceProviderProfileId,
        FlatId? hostFlatId,
        string? purposeOfVisit,
        GateId entryGateId,
        Guid? checkedInBy,
        string? passOrQrNumber,
        string? remarks,
        string? overrideReason,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        AccessCategory = accessCategory;
        GuestProfileId = guestProfileId;
        VehicleId = vehicleId;
        DomesticWorkerProfileId = domesticWorkerProfileId;
        ServiceProviderProfileId = serviceProviderProfileId;
        HostFlatId = hostFlatId;
        PurposeOfVisit = purposeOfVisit;
        EntryGateId = entryGateId;
        EntryAtUtc = nowUtc;
        CheckedInBy = checkedInBy;
        ApprovalStatus = AccessApprovalStatus.Approved;
        PassOrQrNumber = passOrQrNumber;
        Remarks = remarks;
        Status = AccessSessionStatus.CheckedIn;
        OverrideReason = overrideReason;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static AccessSession CheckInGuest(
        Guid tenantId,
        GuestProfileId guestProfileId,
        FlatId hostFlatId,
        string? purposeOfVisit,
        GateId entryGateId,
        Guid? checkedInBy,
        string? passOrQrNumber,
        string? remarks,
        string? overrideReason,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new AccessSession(
            AccessSessionId.New(), tenantId, AccessCategory.Guest, guestProfileId, null, null, null, hostFlatId,
            purposeOfVisit, entryGateId, checkedInBy, passOrQrNumber, remarks, overrideReason, nowUtc);
    }

    public static AccessSession CheckInVehicle(
        Guid tenantId,
        VehicleId vehicleId,
        FlatId? hostFlatId,
        GateId entryGateId,
        Guid? checkedInBy,
        string? remarks,
        string? overrideReason,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new AccessSession(
            AccessSessionId.New(), tenantId, AccessCategory.Vehicle, null, vehicleId, null, null, hostFlatId,
            null, entryGateId, checkedInBy, null, remarks, overrideReason, nowUtc);
    }

    public static AccessSession CheckInDomesticWorker(
        Guid tenantId,
        DomesticWorkerProfileId domesticWorkerProfileId,
        FlatId hostFlatId,
        GateId entryGateId,
        Guid? checkedInBy,
        string? remarks,
        string? overrideReason,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new AccessSession(
            AccessSessionId.New(), tenantId, AccessCategory.DomesticWorker, null, null, domesticWorkerProfileId,
            null, hostFlatId, null, entryGateId, checkedInBy, null, remarks, overrideReason, nowUtc);
    }

    public static AccessSession CheckInServiceProvider(
        Guid tenantId,
        ServiceProviderProfileId serviceProviderProfileId,
        FlatId hostFlatId,
        GateId entryGateId,
        Guid? checkedInBy,
        string? remarks,
        string? overrideReason,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new AccessSession(
            AccessSessionId.New(), tenantId, AccessCategory.ServiceProvider, null, null, null,
            serviceProviderProfileId, hostFlatId, null, entryGateId, checkedInBy, null, remarks, overrideReason, nowUtc);
    }

    public static AccessSession CheckInSebaVisitor(
        Guid tenantId,
        GateId entryGateId,
        Guid? checkedInBy,
        string? purposeOfVisit,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new AccessSession(
            AccessSessionId.New(), tenantId, AccessCategory.SebaVisitor, null, null, null, null, null,
            purposeOfVisit, entryGateId, checkedInBy, null, null, null, nowUtc);
    }

    /// <summary>Closes an open session. Throws if this session was already checked out — checking out
    /// requires an open visit (see mycondo-api docs/conventions and the register-digitization spec).</summary>
    public void CheckOut(GateId exitGateId, Guid? checkedOutBy, DateTimeOffset nowUtc)
    {
        if (Status == AccessSessionStatus.CheckedOut)
        {
            throw new AccessSessionAlreadyClosedException(Id);
        }

        ExitGateId = exitGateId;
        ExitAtUtc = nowUtc;
        CheckedOutBy = checkedOutBy;
        Status = AccessSessionStatus.CheckedOut;
        Version++;
    }
}
