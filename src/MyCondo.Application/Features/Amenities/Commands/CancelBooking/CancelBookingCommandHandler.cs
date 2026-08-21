using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Amenities.Commands.CancelBooking;

/// <summary>
/// If a deposit was already collected (<see cref="Booking.DepositCollectionPostingId"/> set), computes
/// the refund/forfeiture split per the booking's snapshotted cancellation policy — cancelling at or
/// before <see cref="Booking.CancellationDeadlineHours"/> before the event refunds in full; cancelling
/// within the deadline forfeits <see cref="Booking.CancellationDeductionPercentage"/> of it — and posts
/// the settlement ledger entry before calling <see cref="Booking.Cancel"/>. See plan §5.
/// </summary>
public sealed class CancelBookingCommandHandler(
    IBookingRepository bookings,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CancelBookingCommandHandler> logger
) : IRequestHandler<CancelBookingCommand, BookingDto>
{
    public async ValueTask<BookingDto> Handle(CancelBookingCommand command, CancellationToken cancellationToken)
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

        DateTimeOffset nowUtc = clock.UtcNow;

        decimal? refundedAmount = null;
        decimal? deductedAmount = null;
        LedgerPostingId? settlementPostingId = null;

        if (booking.DepositCollectionPostingId is not null && booking.DepositAmount > 0)
        {
            DateTimeOffset deadlineUtc = booking.StartAtUtc.AddHours(-booking.CancellationDeadlineHours);
            bool withinDeadline = nowUtc > deadlineUtc;
            decimal deducted = withinDeadline ? Math.Round(booking.DepositAmount * (booking.CancellationDeductionPercentage / 100m), 2) : 0m;
            decimal refunded = booking.DepositAmount - deducted;
            string description = $"Facility booking cancellation deposit settlement for booking {booking.Id}";

            List<FinancialPostingLine> settlementLines =
            [
                new FinancialPostingLine(LedgerAccountType.RefundableDepositsHeld, null, LedgerDirection.Debit, booking.DepositAmount),
            ];

            if (refunded > 0)
            {
                settlementLines.Add(new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, refunded));
            }

            if (deducted > 0)
            {
                settlementLines.Add(new FinancialPostingLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, deducted));
            }

            FinancialPostingResult settlementPosted = await financialPosting.PostAsync(
                new FinancialPostingRequest(
                    tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description,
                    "FacilityBookingCancellationSettlement", booking.Id.Value, settlementLines),
                cancellationToken);

            refundedAmount = refunded;
            deductedAmount = deducted;
            settlementPostingId = settlementPosted.Posting.Id;
        }

        booking.Cancel(command.Reason, currentUser.UserId, refundedAmount, deductedAmount, settlementPostingId, nowUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Booking {BookingId} cancelled, tenant {TenantId}", id, tenantId);

        return booking.ToDto();
    }
}
