using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnersForTenant;

public sealed class GetFlatOwnersForTenantQueryHandler(
    IFlatOwnershipRepository flatOwnerships,
    IFlatRepository flats,
    IBuildingRepository buildings,
    IUserRepository users,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFlatOwnersForTenantQuery, PagedResult<FlatOwnerRegisterDto>>
{
    public async ValueTask<PagedResult<FlatOwnerRegisterDto>> Handle(
        GetFlatOwnersForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatOwnershipStatus? status = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : Enum.Parse<FlatOwnershipStatus>(query.Status);

        PagedResult<FlatOwnership> page = await flatOwnerships.SearchAsync(
            tenantId, query.Search, status, query.Page, query.PageSize, cancellationToken);

        // Small-tenant scale (a single condominium's owner register) — an in-memory join per unique
        // User/Flat/Building on this page, matching the pattern already used by
        // GetRoleAssignmentsQueryHandler, rather than a bespoke multi-join repository query.
        Dictionary<Guid, User?> ownersById = [];
        Dictionary<Guid, Flat?> flatsById = [];
        Dictionary<Guid, Building?> buildingsById = [];

        List<FlatOwnerRegisterDto> items = [];
        foreach (FlatOwnership ownership in page.Items)
        {
            if (!ownersById.TryGetValue(ownership.UserId, out User? owner))
            {
                owner = await users.GetByIdAsync(new UserId(ownership.UserId), cancellationToken);
                ownersById[ownership.UserId] = owner;
            }

            if (!flatsById.TryGetValue(ownership.FlatId.Value, out Flat? flat))
            {
                flat = await flats.GetByIdAsync(ownership.FlatId, cancellationToken);
                flatsById[ownership.FlatId.Value] = flat;
            }

            if (flat is null || owner is null)
            {
                continue;
            }

            if (!buildingsById.TryGetValue(flat.BuildingId.Value, out Building? building))
            {
                building = await buildings.GetByIdAsync(flat.BuildingId, cancellationToken);
                buildingsById[flat.BuildingId.Value] = building;
            }

            items.Add(new FlatOwnerRegisterDto(
                ownership.Id.Value,
                owner.Id.Value,
                owner.FullName,
                owner.Email,
                flat.Id.Value,
                flat.FlatNumber,
                flat.BuildingId.Value,
                building?.Name ?? "Unknown",
                ownership.Status.ToString(),
                ownership.StartDate,
                ownership.EndDate));
        }

        return new PagedResult<FlatOwnerRegisterDto>(items, page.Page, page.PageSize, page.Total);
    }
}
