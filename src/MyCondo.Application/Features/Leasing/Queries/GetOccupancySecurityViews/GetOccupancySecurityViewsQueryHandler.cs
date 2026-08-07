using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancySecurityViews;

public sealed class GetOccupancySecurityViewsQueryHandler(
    IOccupancyRegistrationRepository registrations,
    IFlatRepository flats,
    IBuildingRepository buildings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetOccupancySecurityViewsQuery, PagedResult<OccupancyRegistrationSecuritySummaryDto>>
{
    public async ValueTask<PagedResult<OccupancyRegistrationSecuritySummaryDto>> Handle(
        GetOccupancySecurityViewsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;

        PagedResult<OccupancyRegistration> result = await registrations.SearchAsync(
            tenantId, flatId, OccupancyRegistrationStatus.Active, query.Page, query.PageSize, cancellationToken);

        List<OccupancyRegistrationSecuritySummaryDto> items = [];
        foreach (OccupancyRegistration registration in result.Items)
        {
            Flat? flat = await flats.GetByIdAsync(registration.FlatId, cancellationToken);
            Building? building = flat is not null ? await buildings.GetByIdAsync(flat.BuildingId, cancellationToken) : null;

            items.Add(new OccupancyRegistrationSecuritySummaryDto(
                registration.Id.Value, flat?.FlatNumber ?? "—", building?.Name ?? "—", registration.PrimaryFullName,
                registration.PrimaryPhotoAttachmentId, registration.Status.ToString(), true));
        }

        return new PagedResult<OccupancyRegistrationSecuritySummaryDto>(items, result.Page, result.PageSize, result.Total);
    }
}
