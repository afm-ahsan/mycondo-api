using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.MeterAssignments.Exceptions;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Domain.Features.Utilities.MeterAssignments;

/// <summary>
/// One entry in a <see cref="Meter"/>'s flat-assignment history — <see cref="AssignedToUtc"/> null
/// means currently assigned. At most one open (null <see cref="AssignedToUtc"/>) assignment per meter
/// is enforced by a partial unique index at the persistence layer; the handler is responsible for
/// calling <see cref="EndAssignment"/> on any existing open assignment before creating a new one.
/// </summary>
public sealed class MeterAssignment : Entity<MeterAssignmentId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public MeterId MeterId { get; private set; }
    public FlatId FlatId { get; private set; }
    public DateTimeOffset AssignedFromUtc { get; private set; }
    public DateTimeOffset? AssignedToUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private MeterAssignment() { }

    private MeterAssignment(
        MeterAssignmentId id, Guid tenantId, MeterId meterId, FlatId flatId, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        MeterId = meterId;
        FlatId = flatId;
        AssignedFromUtc = nowUtc;
        CreatedAtUtc = nowUtc;
    }

    public static MeterAssignment Assign(Guid tenantId, MeterId meterId, FlatId flatId, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new MeterAssignment(MeterAssignmentId.New(), tenantId, meterId, flatId, nowUtc);
    }

    public void EndAssignment(DateTimeOffset nowUtc)
    {
        if (AssignedToUtc is not null)
        {
            throw new MeterAssignmentAlreadyEndedException(Id);
        }

        AssignedToUtc = nowUtc;
    }
}
