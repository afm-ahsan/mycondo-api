using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Operations.Generators;

/// <summary>
/// Physical generator asset master. <see cref="CurrentHourMeterReading"/> is advanced only via
/// <see cref="AdvanceHourMeter"/> (monotonic — never decreases), called from
/// <c>StopGeneratorSessionCommandHandler</c> when an operator records a meter reading at session
/// close, mirroring how <c>Booking</c>'s money fields are only ever mutated through explicit,
/// validated transitions rather than a public setter.
/// </summary>
public sealed class Generator : AggregateRoot<GeneratorId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public string Name { get; private set; }
    public string? Model { get; private set; }
    public decimal? CapacityKva { get; private set; }
    public string? Location { get; private set; }
    public decimal CurrentHourMeterReading { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Generator()
    {
        Name = null!;
    }

    private Generator(
        GeneratorId id,
        Guid tenantId,
        BuildingId buildingId,
        string name,
        string? model,
        decimal? capacityKva,
        string? location,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        BuildingId = buildingId;
        Name = name;
        Model = model;
        CapacityKva = capacityKva;
        Location = location;
        CurrentHourMeterReading = 0m;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Generator Create(
        Guid tenantId, BuildingId buildingId, string name, string? model, decimal? capacityKva, string? location,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (capacityKva is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityKva), "CapacityKva cannot be negative.");
        }

        return new Generator(
            GeneratorId.New(), tenantId, buildingId, name.Trim(), model?.Trim(), capacityKva, location?.Trim(), nowUtc);
    }

    public void UpdateDetails(string name, string? model, decimal? capacityKva, string? location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (capacityKva is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityKva), "CapacityKva cannot be negative.");
        }

        Name = name.Trim();
        Model = model?.Trim();
        CapacityKva = capacityKva;
        Location = location?.Trim();
        Version++;
    }

    /// <summary>Monotonic — a meter physically cannot run backwards. Callers that need to correct a
    /// mis-recorded reading do so via a new, explicitly-authorized entry, not by lowering this value.</summary>
    public void AdvanceHourMeter(decimal newReading)
    {
        if (newReading < CurrentHourMeterReading)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newReading),
                $"Hour meter reading ({newReading}) cannot be lower than the current reading ({CurrentHourMeterReading}).");
        }

        CurrentHourMeterReading = newReading;
        Version++;
    }

    public void Deactivate()
    {
        IsActive = false;
        Version++;
    }

    public void Reactivate()
    {
        IsActive = true;
        Version++;
    }
}
