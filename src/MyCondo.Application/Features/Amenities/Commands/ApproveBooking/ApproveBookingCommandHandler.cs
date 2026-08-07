using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Bookings;

namespace MyCondo.Application.Features.Amenities.Commands.ApproveBooking;

public sealed class ApproveBookingCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ApproveBookingCommandHandler> logger
) : IRequestHandler<ApproveBookingCommand, BookingDto>
{
    public async ValueTask<BookingDto> Handle(ApproveBookingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BookingId id = new(command.BookingId);
        Booking booking = await bookings.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), command.BookingId);
        if (booking.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Booking), command.BookingId);
        }

        booking.Approve(currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Booking {BookingId} approved, tenant {TenantId}", id, tenantId);

        return booking.ToDto();
    }
}
