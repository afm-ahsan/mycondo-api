using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.Common;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.CancelBooking;

public sealed record CancelBookingCommand(Guid BookingId, string Reason)
    : IRequest<BookingDto>, IHasBookingId, IRequiresResolvedFeature;
