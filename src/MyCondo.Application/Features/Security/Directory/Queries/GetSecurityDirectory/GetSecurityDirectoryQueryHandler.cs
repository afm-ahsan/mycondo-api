using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Directory.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectory;

/// <summary>
/// Merges active <see cref="OccupancyRegistration"/> (Tenant) and active <see cref="FlatOwnership"/>
/// (Owner) rows into a single security-facing directory. Small-tenant scale (a single condominium's
/// resident register) — an in-memory merge/filter/paginate over both sources' full active sets, matching
/// the pattern already used by <c>GetFlatOwnersForTenantQueryHandler</c> and the retired
/// <c>GetOccupancySecurityViewsQueryHandler</c>, rather than a bespoke cross-aggregate SQL union.
/// </summary>
public sealed class GetSecurityDirectoryQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IFlatOwnershipRepository flatOwnerships,
    IResidentRepository residents,
    IFlatRepository flats,
    IBuildingRepository buildings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetSecurityDirectoryQuery, PagedResult<SecurityDirectoryEntryDto>>
{
    // An in-memory merge assumes both active sets fit in memory — true at single-condominium scale
    // (see class doc comment); this caps a pathological fetch rather than truly supporting unbounded
    // tenants.
    private const int MaxSourceRows = 10_000;

    public async ValueTask<PagedResult<SecurityDirectoryEntryDto>> Handle(
        GetSecurityDirectoryQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;

        PagedResult<OccupancyRegistration> tenantRows = await registrations.SearchAsync(
            tenantId, flatId, OccupancyRegistrationStatus.Active, null, 1, MaxSourceRows, cancellationToken);
        PagedResult<FlatOwnership> ownerRows = await flatOwnerships.SearchAsync(
            tenantId, null, FlatOwnershipStatus.Active, 1, MaxSourceRows, cancellationToken);

        Dictionary<Guid, Flat?> flatsById = [];
        Dictionary<Guid, Building?> buildingsById = [];
        Dictionary<Guid, Resident?> residentsById = [];

        async ValueTask<Flat?> ResolveFlatAsync(FlatId id)
        {
            if (!flatsById.TryGetValue(id.Value, out Flat? flat))
            {
                flat = await flats.GetByIdAsync(id, cancellationToken);
                flatsById[id.Value] = flat;
            }

            return flat;
        }

        async ValueTask<Building?> ResolveBuildingAsync(BuildingId id)
        {
            if (!buildingsById.TryGetValue(id.Value, out Building? building))
            {
                building = await buildings.GetByIdAsync(id, cancellationToken);
                buildingsById[id.Value] = building;
            }

            return building;
        }

        async ValueTask<Resident?> ResolveResidentAsync(Guid id)
        {
            if (!residentsById.TryGetValue(id, out Resident? resident))
            {
                resident = await residents.GetByIdAsync(new ResidentId(id), cancellationToken);
                residentsById[id] = resident;
            }

            return resident;
        }

        List<(SecurityDirectoryEntryDto Entry, string? Phone)> merged = [];

        foreach (OccupancyRegistration registration in tenantRows.Items)
        {
            Flat? flat = await ResolveFlatAsync(registration.FlatId);
            if (flat is null || (query.BuildingId is Guid buildingFilter && flat.BuildingId.Value != buildingFilter))
            {
                continue;
            }

            Building? building = await ResolveBuildingAsync(flat.BuildingId);
            merged.Add((
                new SecurityDirectoryEntryDto(
                    registration.Id.Value, "Tenant", flat.Id.Value, flat.FlatNumber, flat.BuildingId.Value,
                    building?.Name ?? "—", registration.PrimaryFullName, registration.PrimaryPhotoAttachmentId,
                    "Authorized", registration.Status.ToString()),
                registration.PrimaryPhone));
        }

        foreach (FlatOwnership ownership in ownerRows.Items)
        {
            if (flatId is not null && ownership.FlatId.Value != flatId.Value.Value)
            {
                continue;
            }

            Flat? flat = await ResolveFlatAsync(ownership.FlatId);
            Resident? owner = await ResolveResidentAsync(ownership.ResidentId);
            if (flat is null || owner is null
                || (query.BuildingId is Guid buildingFilter && flat.BuildingId.Value != buildingFilter))
            {
                continue;
            }

            Building? building = await ResolveBuildingAsync(flat.BuildingId);
            bool authorized = ownership.Status == FlatOwnershipStatus.Active;
            merged.Add((
                new SecurityDirectoryEntryDto(
                    ownership.Id.Value, "Owner", flat.Id.Value, flat.FlatNumber, flat.BuildingId.Value,
                    building?.Name ?? "—", owner.FullName, null, authorized ? "Authorized" : "Revoked",
                    ownership.Status.ToString()),
                owner.Phone));
        }

        IEnumerable<(SecurityDirectoryEntryDto Entry, string? Phone)> filtered = merged;

        if (!string.IsNullOrWhiteSpace(query.AccessStatus))
        {
            filtered = filtered.Where(m => string.Equals(m.Entry.AccessStatus, query.AccessStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            filtered = filtered.Where(m =>
                m.Entry.PrimaryFullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || m.Entry.FlatNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (m.Phone is not null && m.Phone.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        List<SecurityDirectoryEntryDto> ordered = filtered
            .OrderBy(m => m.Entry.BuildingName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Entry.FlatNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Entry.PrimaryFullName, StringComparer.OrdinalIgnoreCase)
            .Select(m => m.Entry)
            .ToList();

        int total = ordered.Count;
        List<SecurityDirectoryEntryDto> page = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResult<SecurityDirectoryEntryDto>(page, query.Page, query.PageSize, total);
    }
}
