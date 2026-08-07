using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;

/// <summary>
/// Maintenance due tracking for a <see cref="Generator"/>. Due can be based on date, runtime, or both
/// (register-digitization spec §5.13) — <see cref="IsDue"/> evaluates both independently, either one
/// being met is enough. Advancing past a due date/reading happens via
/// <c>CompleteMaintenanceServiceCommandHandler</c> creating a
/// <c>GeneratorServiceRecords.GeneratorServiceRecord</c> and calling <see cref="Reschedule"/> in the
/// same transaction, not automatically.
/// </summary>
public sealed class GeneratorMaintenanceSchedule : Entity<GeneratorMaintenanceScheduleId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public GeneratorId GeneratorId { get; private set; }
    public DateOnly? NextDueDate { get; private set; }
    public decimal? NextDueHourMeterReading { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private GeneratorMaintenanceSchedule() { }

    private GeneratorMaintenanceSchedule(
        GeneratorMaintenanceScheduleId id, Guid tenantId, GeneratorId generatorId, DateOnly? nextDueDate,
        decimal? nextDueHourMeterReading, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        GeneratorId = generatorId;
        NextDueDate = nextDueDate;
        NextDueHourMeterReading = nextDueHourMeterReading;
        IsActive = true;
        CreatedAtUtc = nowUtc;
    }

    public static GeneratorMaintenanceSchedule Create(
        Guid tenantId, GeneratorId generatorId, DateOnly? nextDueDate, decimal? nextDueHourMeterReading, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (nextDueDate is null && nextDueHourMeterReading is null)
        {
            throw new ArgumentException("At least one of NextDueDate or NextDueHourMeterReading is required.");
        }

        if (nextDueHourMeterReading is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextDueHourMeterReading), "NextDueHourMeterReading cannot be negative.");
        }

        return new GeneratorMaintenanceSchedule(
            GeneratorMaintenanceScheduleId.New(), tenantId, generatorId, nextDueDate, nextDueHourMeterReading, nowUtc);
    }

    public void Reschedule(DateOnly? nextDueDate, decimal? nextDueHourMeterReading)
    {
        if (nextDueDate is null && nextDueHourMeterReading is null)
        {
            throw new ArgumentException("At least one of NextDueDate or NextDueHourMeterReading is required.");
        }

        if (nextDueHourMeterReading is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextDueHourMeterReading), "NextDueHourMeterReading cannot be negative.");
        }

        NextDueDate = nextDueDate;
        NextDueHourMeterReading = nextDueHourMeterReading;
    }

    public void Deactivate() => IsActive = false;

    public bool IsDue(DateOnly today, decimal currentHourMeterReading) =>
        IsActive
        && ((NextDueDate is DateOnly dueDate && today >= dueDate)
            || (NextDueHourMeterReading is decimal dueReading && currentHourMeterReading >= dueReading));
}
