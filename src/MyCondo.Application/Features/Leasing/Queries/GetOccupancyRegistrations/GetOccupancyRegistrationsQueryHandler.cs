using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrations;

public sealed class GetOccupancyRegistrationsQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IFlatRepository flats,
    IBuildingRepository buildings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetOccupancyRegistrationsQuery, PagedResult<OccupancyRegistrationListItemDto>>
{
    public async ValueTask<PagedResult<OccupancyRegistrationListItemDto>> Handle(
        GetOccupancyRegistrationsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;
        OccupancyRegistrationStatus? status = query.Status is string rawStatus
            ? Enum.Parse<OccupancyRegistrationStatus>(rawStatus)
            : null;

        PagedResult<OccupancyRegistration> result = await registrations.SearchAsync(
            tenantId, flatId, status, query.Search, query.Page, query.PageSize, cancellationToken);

        // Small-tenant scale (a single condominium's registration list) — an in-memory join per
        // unique Flat/Building on this page, matching the pattern already used by
        // GetFlatOwnersForTenantQueryHandler, rather than a bespoke multi-join repository query.
        Dictionary<Guid, Flat?> flatsById = [];
        Dictionary<Guid, Building?> buildingsById = [];

        List<OccupancyRegistrationListItemDto> items = [];
        foreach (OccupancyRegistration registration in result.Items)
        {
            if (!flatsById.TryGetValue(registration.FlatId.Value, out Flat? flat))
            {
                flat = await flats.GetByIdAsync(registration.FlatId, cancellationToken);
                flatsById[registration.FlatId.Value] = flat;
            }

            Building? building = null;
            if (flat is not null && !buildingsById.TryGetValue(flat.BuildingId.Value, out building))
            {
                building = await buildings.GetByIdAsync(flat.BuildingId, cancellationToken);
                buildingsById[flat.BuildingId.Value] = building;
            }

            items.Add(new OccupancyRegistrationListItemDto(
                registration.Id.Value,
                registration.PrimaryFullName,
                registration.PrimaryEmail,
                registration.PrimaryPhone,
                registration.FlatId.Value,
                flat?.FlatNumber ?? "Unknown",
                flat?.BuildingId.Value ?? Guid.Empty,
                building?.Name ?? "Unknown",
                registration.OccupancyType.ToString(),
                registration.Status.ToString(),
                registration.MoveInExpectedDate));
        }

        return new PagedResult<OccupancyRegistrationListItemDto>(items, result.Page, result.PageSize, result.Total);
    }
}
