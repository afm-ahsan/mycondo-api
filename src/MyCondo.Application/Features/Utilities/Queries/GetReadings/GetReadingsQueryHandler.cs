using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadings;

public sealed class GetReadingsQueryHandler(
    IReadingRepository readings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetReadingsQuery, PagedResult<ReadingDto>>
{
    public async ValueTask<PagedResult<ReadingDto>> Handle(GetReadingsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        MeterId? meterId = query.MeterId is Guid rawMeterId ? new MeterId(rawMeterId) : null;
        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;
        ReadingStatus? status = query.Status is null ? null : Enum.Parse<ReadingStatus>(query.Status);

        PagedResult<Reading> result = await readings.SearchAsync(
            tenantId, meterId, flatId, status, query.Page, query.PageSize, cancellationToken);

        List<ReadingDto> items = result.Items.Select(r => r.ToDto()).ToList();

        return new PagedResult<ReadingDto>(items, result.Page, result.PageSize, result.Total);
    }
}
