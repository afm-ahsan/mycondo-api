using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Amenities.Commands.RequestBooking;

public sealed class RequestBookingCommandHandler(
    IFacilityRepository facilities,
    IBlackoutDateRepository blackoutDates,
    IBookingRepository bookings,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RequestBookingCommandHandler> logger
) : IRequestHandler<RequestBookingCommand, BookingDto>
{
    public async ValueTask<BookingDto> Handle(RequestBookingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId facilityId = new(command.FacilityId);
        Facility facility = await facilities.GetByIdAsync(facilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), command.FacilityId);
        if (facility.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Facility), command.FacilityId);
        }

        if (!facility.IsActive)
        {
            throw new ConflictException($"Facility {command.FacilityId} is inactive and cannot be booked.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        if (command.ExpectedGuestCount > facility.Capacity)
        {
            throw new ConflictException(
                $"ExpectedGuestCount ({command.ExpectedGuestCount}) exceeds facility capacity ({facility.Capacity}).");
        }

        IReadOnlyList<BlackoutDate> activeBlackouts = await blackoutDates.GetActiveForFacilityAsync(tenantId, facilityId, cancellationToken);
        DateOnly startDate = DateOnly.FromDateTime(command.StartAtUtc.UtcDateTime);
        DateOnly endDate = DateOnly.FromDateTime(command.EndAtUtc.UtcDateTime);
        bool blackedOut = activeBlackouts.Any(b => b.DateFrom <= endDate && b.DateTo >= startDate);
        if (blackedOut)
        {
            throw new ConflictException($"Facility {command.FacilityId} has a blackout covering the requested date range.");
        }

        DateTimeOffset effectiveStartUtc = command.StartAtUtc.AddMinutes(-command.SetupBufferMinutes);
        DateTimeOffset effectiveEndUtc = command.EndAtUtc.AddMinutes(command.CleanupBufferMinutes);
        bool overlaps = await bookings.HasOverlappingBookingAsync(tenantId, facilityId, effectiveStartUtc, effectiveEndUtc, cancellationToken);
        if (overlaps)
        {
            throw new ConflictException($"Facility {command.FacilityId} already has a booking overlapping the requested window.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        Booking booking = Booking.Request(
            tenantId, facilityId, facility.BuildingId, flatId, command.EventType, command.StartAtUtc, command.EndAtUtc,
            command.SetupBufferMinutes, command.CleanupBufferMinutes, command.ExpectedGuestCount, facility.RequiresApproval,
            facility.BookingChargeAmount ?? 0m, facility.DepositAmount ?? 0m, facility.CancellationDeadlineHours,
            facility.CancellationDeductionPercentage, command.TermsAccepted ? nowUtc : null, nowUtc);

        bookings.Add(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Booking {BookingId} requested for facility {FacilityId}, flat {FlatId}, tenant {TenantId}",
            booking.Id, facilityId, flatId, tenantId);

        return booking.ToDto();
    }
}
