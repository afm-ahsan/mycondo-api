using System.Text.RegularExpressions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Security.Vehicles;

public sealed partial class Vehicle : AggregateRoot<VehicleId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string RegistrationNumber { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public string? Make { get; private set; }
    public string? Model { get; private set; }
    public string? Color { get; private set; }
    public VehicleOwnershipCategory OwnershipCategory { get; private set; }
    public FlatId? FlatId { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? BlockReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    private Vehicle()
    {
        RegistrationNumber = null!;
    }

    private Vehicle(
        VehicleId id,
        Guid tenantId,
        string registrationNumber,
        VehicleType vehicleType,
        string? make,
        string? model,
        string? color,
        VehicleOwnershipCategory ownershipCategory,
        FlatId? flatId,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        RegistrationNumber = registrationNumber;
        VehicleType = vehicleType;
        Make = make;
        Model = model;
        Color = color;
        OwnershipCategory = ownershipCategory;
        FlatId = flatId;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Vehicle Register(
        Guid tenantId,
        string registrationNumber,
        VehicleType vehicleType,
        string? make,
        string? model,
        string? color,
        VehicleOwnershipCategory ownershipCategory,
        FlatId? flatId,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationNumber);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Vehicle(
            VehicleId.New(), tenantId, NormalizeRegistrationNumber(registrationNumber), vehicleType, make?.Trim(),
            model?.Trim(), color?.Trim(), ownershipCategory, flatId, nowUtc);
    }

    /// <summary>Uppercase, whitespace-stripped — so "dha metro-ga 12 3456" and "DHA METRO GA 123456"
    /// resolve to the same registration for duplicate-detection purposes.</summary>
    public static string NormalizeRegistrationNumber(string registrationNumber) =>
        WhitespaceRegex().Replace(registrationNumber.Trim().ToUpperInvariant(), string.Empty);

    public void Block(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsBlocked = true;
        BlockReason = reason.Trim();
        Version++;
    }

    public void Unblock()
    {
        IsBlocked = false;
        BlockReason = null;
        Version++;
    }

    public void Deactivate(DateTimeOffset nowUtc, Guid? deactivatedBy)
    {
        if (DeletedAtUtc is not null)
        {
            return;
        }

        DeletedAtUtc = nowUtc;
        DeletedBy = deactivatedBy;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
