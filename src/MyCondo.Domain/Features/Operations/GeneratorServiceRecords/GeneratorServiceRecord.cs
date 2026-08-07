using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorServiceRecords;

/// <summary>Completed-maintenance service history entry for a <see cref="Generator"/>. Immutable once
/// recorded — the audit trail of what maintenance was actually done.</summary>
public sealed class GeneratorServiceRecord : Entity<GeneratorServiceRecordId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public GeneratorId GeneratorId { get; private set; }
    public DateTimeOffset PerformedAtUtc { get; private set; }
    public string Description { get; private set; }
    public decimal? Cost { get; private set; }
    public Guid? PerformedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private GeneratorServiceRecord()
    {
        Description = null!;
    }

    private GeneratorServiceRecord(
        GeneratorServiceRecordId id, Guid tenantId, GeneratorId generatorId, DateTimeOffset performedAtUtc,
        string description, decimal? cost, Guid? performedBy, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        GeneratorId = generatorId;
        PerformedAtUtc = performedAtUtc;
        Description = description;
        Cost = cost;
        PerformedBy = performedBy;
        CreatedAtUtc = nowUtc;
    }

    public static GeneratorServiceRecord Record(
        Guid tenantId, GeneratorId generatorId, DateTimeOffset performedAtUtc, string description, decimal? cost,
        Guid? performedBy, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (cost is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Cost cannot be negative.");
        }

        return new GeneratorServiceRecord(
            GeneratorServiceRecordId.New(), tenantId, generatorId, performedAtUtc, description.Trim(), cost, performedBy, nowUtc);
    }
}
