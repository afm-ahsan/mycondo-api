using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Common.Services;

public sealed class FlatAccessAuthorizer(
    IFlatOwnershipRepository flatOwnerships,
    IResidentRepository residents,
    IOccupancyRegistrationRepository occupancyRegistrations,
    IFlatRepository flats
) : IFlatAccessAuthorizer
{
    /// <summary>A <c>User</c> is never referenced by <see cref="FlatOwnership"/> directly — ownership
    /// is keyed on <see cref="Resident"/> so an owner's profile can exist without a portal account. A
    /// logged-in User's ownership is resolved by first finding every Resident bridged to that User
    /// (<see cref="Resident.UserId"/>), then checking each for an active ownership on this Flat.</summary>
    public async Task<bool> HasActiveOwnershipAsync(Guid tenantId, Guid userId, Guid flatId, CancellationToken cancellationToken)
    {
        List<Resident> userResidents = await residents.GetByUserIdAsync(tenantId, userId, cancellationToken);
        foreach (Resident resident in userResidents)
        {
            if (await flatOwnerships.ExistsActiveForResidentAndFlatAsync(
                tenantId, resident.Id.Value, new FlatId(flatId), cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> HasActiveOccupancyAsync(Guid tenantId, Guid userId, Guid flatId, CancellationToken cancellationToken)
    {
        List<Resident> userResidents = await residents.GetByUserIdAsync(tenantId, userId, cancellationToken);
        Resident? resident = userResidents.FirstOrDefault(r => r.FlatId.Value == flatId);
        if (resident is null)
        {
            return false;
        }

        OccupancyRegistration? active = await occupancyRegistrations.GetActiveForFlatAsync(tenantId, resident.FlatId, cancellationToken);
        return active is not null && active.PrimaryResidentId == resident.Id;
    }

    public async Task<List<FlatRelationship>> GetActiveRelationshipsAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        List<FlatRelationship> relationships = [];

        List<Resident> userResidents = await residents.GetByUserIdAsync(tenantId, userId, cancellationToken);

        foreach (Resident resident in userResidents)
        {
            List<FlatOwnership> ownerships = await flatOwnerships.GetActiveForResidentAsync(
                tenantId, resident.Id.Value, cancellationToken);
            foreach (FlatOwnership ownership in ownerships)
            {
                Flat? flat = await flats.GetByIdAsync(ownership.FlatId, cancellationToken);
                if (flat is null)
                {
                    continue;
                }

                relationships.Add(new FlatRelationship(
                    ownership.FlatId.Value, flat.BuildingId.Value, FlatRelationshipKind.Ownership,
                    ownership.StartDate, ownership.EndDate));
            }

            OccupancyRegistration? active = await occupancyRegistrations.GetActiveForFlatAsync(tenantId, resident.FlatId, cancellationToken);
            if (active is null || active.PrimaryResidentId != resident.Id)
            {
                continue;
            }

            Flat? occupiedFlat = await flats.GetByIdAsync(resident.FlatId, cancellationToken);
            if (occupiedFlat is null)
            {
                continue;
            }

            relationships.Add(new FlatRelationship(
                resident.FlatId.Value, occupiedFlat.BuildingId.Value, FlatRelationshipKind.Occupancy,
                active.MoveInExpectedDate, null));
        }

        return relationships;
    }
}
