using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.RequestBooking;

public sealed record RequestBookingCommand(
    Guid FacilityId,
    Guid FlatId,
    string EventType,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    int SetupBufferMinutes,
    int CleanupBufferMinutes,
    int ExpectedGuestCount,
    bool TermsAccepted
) : IRequest<BookingDto>;
