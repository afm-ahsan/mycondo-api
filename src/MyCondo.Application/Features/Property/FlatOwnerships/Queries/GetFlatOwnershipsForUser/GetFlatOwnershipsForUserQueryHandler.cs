using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForUser;

public sealed class GetFlatOwnershipsForUserQueryHandler(
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    IBuildingRepository buildings,
    IUserRepository users,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFlatOwnershipsForUserQuery, List<UserFlatOwnershipDto>>
{
    public async ValueTask<List<UserFlatOwnershipDto>> Handle(
        GetFlatOwnershipsForUserQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        UserId userId = new(query.UserId);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), query.UserId);

        if (user.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(User), query.UserId);
        }

        List<FlatOwnership> ownerships = await flatOwnerships.GetAllForUserAsync(tenantId, query.UserId, cancellationToken);

        List<UserFlatOwnershipDto> items = [];
        foreach (FlatOwnership ownership in ownerships)
        {
            Flat? flat = await flats.GetByIdAsync(ownership.FlatId, cancellationToken);
            if (flat is null)
            {
                continue;
            }

            Building? building = await buildings.GetByIdAsync(flat.BuildingId, cancellationToken);

            items.Add(new UserFlatOwnershipDto(
                ownership.Id.Value,
                flat.Id.Value,
                flat.FlatNumber,
                flat.BuildingId.Value,
                building?.Name ?? "Unknown",
                ownership.Status.ToString(),
                ownership.StartDate,
                ownership.EndDate));
        }

        return items;
    }
}
