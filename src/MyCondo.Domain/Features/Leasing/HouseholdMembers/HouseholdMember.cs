using MyCondo.Domain.Common;
using MyCondo.Domain.Common.PhoneNumbers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.Features.Leasing.HouseholdMembers;

/// <summary>
/// A family member/co-occupant listed on an <see cref="OccupancyRegistration"/> — deliberately its own
/// entity with its own repository (matching this codebase's established child-entity convention, see
/// e.g. <c>GeneratorFuelReceipt</c>) rather than an EF owned collection, so members can be added/
/// deactivated independently without reloading the whole registration aggregate.
/// </summary>
public sealed class HouseholdMember : Entity<HouseholdMemberId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public OccupancyRegistrationId OccupancyRegistrationId { get; private set; }
    public string FullName { get; private set; }
    public string RelationshipToPrimary { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Phone { get; private set; }
    public string? NationalIdNumber { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private HouseholdMember()
    {
        FullName = null!;
        RelationshipToPrimary = null!;
    }

    private HouseholdMember(
        HouseholdMemberId id, Guid tenantId, OccupancyRegistrationId occupancyRegistrationId, string fullName,
        string relationshipToPrimary, DateOnly? dateOfBirth, string? phone, string? nationalIdNumber,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        OccupancyRegistrationId = occupancyRegistrationId;
        FullName = fullName;
        RelationshipToPrimary = relationshipToPrimary;
        DateOfBirth = dateOfBirth;
        Phone = phone;
        NationalIdNumber = nationalIdNumber;
        IsActive = true;
        CreatedAtUtc = nowUtc;
    }

    public static HouseholdMember Add(
        Guid tenantId, OccupancyRegistrationId occupancyRegistrationId, string fullName,
        string relationshipToPrimary, DateOnly? dateOfBirth, string? phone, string? nationalIdNumber,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(relationshipToPrimary);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new HouseholdMember(
            HouseholdMemberId.New(), tenantId, occupancyRegistrationId, fullName.Trim(),
            relationshipToPrimary.Trim(), dateOfBirth, BangladeshMobileNumber.Normalize(phone),
            nationalIdNumber?.Trim(), nowUtc);
    }

    /// <summary>Removes this member from the active household without moving out the whole
    /// registration — e.g. a family member relocating independently.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
