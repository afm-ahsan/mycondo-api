using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Services;

/// <summary>Tenant-occupant-first resolution: the flat's active <see cref="OccupancyRegistration"/>
/// (the actual resident named responsible for occupying the flat) if one exists, otherwise the flat's
/// active <see cref="FlatOwnership"/> owner. Neither found is not an error — see
/// <see cref="ResponsiblePartySnapshot"/>. This is a display/audit default, not an authoritative
/// billing-liability policy; who actually owes an association for charges remains governed by
/// existing business practice/contract, unaffected by this snapshot.</summary>
public sealed class ResponsiblePartyResolver(
    IFlatOwnershipRepository flatOwnerships,
    IOccupancyRegistrationRepository occupancyRegistrations
) : IResponsiblePartyResolver
{
    public async Task<ResponsiblePartySnapshot?> ResolveAsync(
        Guid tenantId, FlatId flatId, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        OccupancyRegistration? activeOccupancy =
            await occupancyRegistrations.GetActiveForFlatAsync(tenantId, flatId, cancellationToken);
        if (activeOccupancy is not null)
        {
            return new ResponsiblePartySnapshot(
                ResponsiblePartyType.Tenant, activeOccupancy.PrimaryResidentId.Value, FlatOwnershipId: null,
                activeOccupancy.Id.Value);
        }

        List<FlatOwnership> ownerships = await flatOwnerships.GetForFlatAsync(tenantId, flatId, cancellationToken);
        FlatOwnership? activeOwnership = ownerships
            .Where(o => o.Status == FlatOwnershipStatus.Active
                && o.StartDate <= asOfDate
                && (o.EndDate is null || o.EndDate >= asOfDate))
            .OrderByDescending(o => o.StartDate)
            .FirstOrDefault();

        return activeOwnership is null
            ? null
            : new ResponsiblePartySnapshot(
                ResponsiblePartyType.Owner, activeOwnership.ResidentId, activeOwnership.Id.Value,
                OccupancyRegistrationId: null);
    }
}
