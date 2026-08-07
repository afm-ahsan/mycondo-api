using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorSessions.Exceptions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorSessions;

/// <summary>
/// A single start→stop runtime log entry for a <see cref="Generator"/>. <see cref="RuntimeMinutes"/>
/// is computed server-side on <see cref="Stop"/> from <see cref="StartAtUtc"/>/<see cref="StopAtUtc"/>
/// — never client-supplied, per the register-digitization spec's "Runtime is calculated server-side"
/// business rule. "Only one open session per generator" is enforced at the handler level (row lock on
/// the owning <see cref="Generator"/>), not here — a single session has no visibility into its
/// siblings.
/// </summary>
public sealed class GeneratorSession : AggregateRoot<GeneratorSessionId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public GeneratorId GeneratorId { get; private set; }
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset? StopAtUtc { get; private set; }
    public Guid? OperatorId { get; private set; }
    public decimal OpeningFuelLevel { get; private set; }
    public decimal? ClosingFuelLevel { get; private set; }
    public string? OutageReason { get; private set; }
    public int? RuntimeMinutes { get; private set; }
    public GeneratorSessionStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private GeneratorSession() { }

    private GeneratorSession(
        GeneratorSessionId id,
        Guid tenantId,
        GeneratorId generatorId,
        Guid? operatorId,
        decimal openingFuelLevel,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        GeneratorId = generatorId;
        StartAtUtc = nowUtc;
        OperatorId = operatorId;
        OpeningFuelLevel = openingFuelLevel;
        Status = GeneratorSessionStatus.Open;
        CreatedAtUtc = nowUtc;
    }

    public static GeneratorSession Start(
        Guid tenantId, GeneratorId generatorId, Guid? operatorId, decimal openingFuelLevel, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (openingFuelLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openingFuelLevel), "OpeningFuelLevel cannot be negative.");
        }

        return new GeneratorSession(GeneratorSessionId.New(), tenantId, generatorId, operatorId, openingFuelLevel, nowUtc);
    }

    public void Stop(decimal closingFuelLevel, string? outageReason, DateTimeOffset nowUtc)
    {
        if (Status != GeneratorSessionStatus.Open)
        {
            throw new GeneratorSessionAlreadyClosedException(Id);
        }

        if (closingFuelLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(closingFuelLevel), "ClosingFuelLevel cannot be negative.");
        }

        if (nowUtc < StartAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(nowUtc), "Stop time cannot precede start time.");
        }

        StopAtUtc = nowUtc;
        ClosingFuelLevel = closingFuelLevel;
        OutageReason = outageReason?.Trim();
        RuntimeMinutes = (int)(nowUtc - StartAtUtc).TotalMinutes;
        Status = GeneratorSessionStatus.Closed;
    }
}
