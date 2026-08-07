using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrations;

public sealed class GetOccupancyRegistrationsQueryHandler(
    IOccupancyRegistrationRepository registrations,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetOccupancyRegistrationsQuery, PagedResult<OccupancyRegistrationDto>>
{
    public async ValueTask<PagedResult<OccupancyRegistrationDto>> Handle(
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
            tenantId, flatId, status, query.Page, query.PageSize, cancellationToken);

        List<OccupancyRegistrationDto> items = result.Items.Select(r => r.ToDto()).ToList();

        return new PagedResult<OccupancyRegistrationDto>(items, result.Page, result.PageSize, result.Total);
    }
}
