using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.Common;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookingById;

public sealed record GetBookingByIdQuery(Guid BookingId)
    : IRequest<BookingDto>, IHasBookingId, IRequiresResolvedFeature, ILifecycleReadOperation;
