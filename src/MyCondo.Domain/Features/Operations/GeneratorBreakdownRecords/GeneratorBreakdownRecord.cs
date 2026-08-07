using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords.Exceptions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;

/// <summary>Breakdown/outage log entry for a <see cref="Generator"/>. Reported open (no
/// <see cref="DowntimeEndUtc"/>/<see cref="Resolution"/>), then <see cref="Resolve"/>d once the
/// generator is back in service.</summary>
public sealed class GeneratorBreakdownRecord : Entity<GeneratorBreakdownRecordId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public GeneratorId GeneratorId { get; private set; }
    public DateTimeOffset ReportedAtUtc { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset DowntimeStartUtc { get; private set; }
    public DateTimeOffset? DowntimeEndUtc { get; private set; }
    public string? Resolution { get; private set; }
    public decimal? Cost { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private GeneratorBreakdownRecord()
    {
        Description = null!;
    }

    private GeneratorBreakdownRecord(
        GeneratorBreakdownRecordId id, Guid tenantId, GeneratorId generatorId, DateTimeOffset reportedAtUtc,
        string description, DateTimeOffset downtimeStartUtc, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        GeneratorId = generatorId;
        ReportedAtUtc = reportedAtUtc;
        Description = description;
        DowntimeStartUtc = downtimeStartUtc;
        CreatedAtUtc = nowUtc;
    }

    public static GeneratorBreakdownRecord Report(
        Guid tenantId, GeneratorId generatorId, DateTimeOffset reportedAtUtc, string description,
        DateTimeOffset downtimeStartUtc, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new GeneratorBreakdownRecord(
            GeneratorBreakdownRecordId.New(), tenantId, generatorId, reportedAtUtc, description.Trim(), downtimeStartUtc, nowUtc);
    }

    public void Resolve(string resolution, decimal? cost, DateTimeOffset downtimeEndUtc)
    {
        if (DowntimeEndUtc is not null)
        {
            throw new GeneratorBreakdownAlreadyResolvedException(Id);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(resolution);
        if (cost is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Cost cannot be negative.");
        }

        if (downtimeEndUtc < DowntimeStartUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(downtimeEndUtc), "Downtime end cannot precede downtime start.");
        }

        Resolution = resolution.Trim();
        Cost = cost;
        DowntimeEndUtc = downtimeEndUtc;
    }
}
