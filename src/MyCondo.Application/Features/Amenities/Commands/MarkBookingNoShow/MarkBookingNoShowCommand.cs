using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.Common;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.MarkBookingNoShow;

public sealed record MarkBookingNoShowCommand(Guid BookingId)
    : IRequest<BookingDto>, IHasBookingId, IRequiresResolvedFeature, ILifecycleWriteOperation;
