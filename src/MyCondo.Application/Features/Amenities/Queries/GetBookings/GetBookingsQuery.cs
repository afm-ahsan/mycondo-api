using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookings;

public sealed record GetBookingsQuery(
    Guid? FacilityId,
    Guid? FlatId,
    string? Status,
    Guid? BuildingId,
    string? EventType,
    string? PaymentStatus,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int Page,
    int PageSize
) : IRequest<PagedResult<BookingDto>>;
