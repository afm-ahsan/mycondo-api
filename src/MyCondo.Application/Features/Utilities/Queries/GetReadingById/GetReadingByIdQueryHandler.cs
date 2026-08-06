using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadingById;

public sealed class GetReadingByIdQueryHandler(
    IReadingRepository readings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetReadingByIdQuery, ReadingDto>
{
    public async ValueTask<ReadingDto> Handle(GetReadingByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ReadingId id = new(query.ReadingId);
        Reading reading = await readings.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Reading), query.ReadingId);
        if (reading.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Reading), query.ReadingId);
        }

        return reading.ToDto();
    }
}
