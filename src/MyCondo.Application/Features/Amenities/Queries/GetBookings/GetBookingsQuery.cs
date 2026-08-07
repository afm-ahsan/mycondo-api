using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookings;

public sealed record GetBookingsQuery(
    Guid? FacilityId,
    Guid? FlatId,
    string? Status,
    int Page,
    int PageSize
) : IRequest<PagedResult<BookingDto>>;
