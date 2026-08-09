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
    public Task<bool> HasActiveOwnershipAsync(Guid tenantId, Guid userId, Guid flatId, CancellationToken cancellationToken) =>
        flatOwnerships.ExistsActiveForUserAndFlatAsync(tenantId, userId, new FlatId(flatId), cancellationToken);

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

        List<FlatOwnership> ownerships = await flatOwnerships.GetActiveForUserAsync(tenantId, userId, cancellationToken);
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

        List<Resident> userResidents = await residents.GetByUserIdAsync(tenantId, userId, cancellationToken);
        foreach (Resident resident in userResidents)
        {
            OccupancyRegistration? active = await occupancyRegistrations.GetActiveForFlatAsync(tenantId, resident.FlatId, cancellationToken);
            if (active is null || active.PrimaryResidentId != resident.Id)
            {
                continue;
            }

            Flat? flat = await flats.GetByIdAsync(resident.FlatId, cancellationToken);
            if (flat is null)
            {
                continue;
            }

            relationships.Add(new FlatRelationship(
                resident.FlatId.Value, flat.BuildingId.Value, FlatRelationshipKind.Occupancy,
                active.MoveInExpectedDate, null));
        }

        return relationships;
    }
}
