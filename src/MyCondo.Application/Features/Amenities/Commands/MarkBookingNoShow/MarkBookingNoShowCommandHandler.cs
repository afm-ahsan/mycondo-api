using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Amenities.Commands.MarkBookingNoShow;

/// <summary>
/// Treated as always within the cancellation deadline (see <see cref="Booking.MarkNoShow"/>'s doc
/// comment) — the full <see cref="Booking.CancellationDeductionPercentage"/> of any collected deposit
/// is forfeited, posted the same way <c>CancelBookingCommandHandler</c> posts a within-deadline
/// cancellation settlement.
/// </summary>
public sealed class MarkBookingNoShowCommandHandler(
    IBookingRepository bookings,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<MarkBookingNoShowCommandHandler> logger
) : IRequestHandler<MarkBookingNoShowCommand, BookingDto>
{
    public async ValueTask<BookingDto> Handle(MarkBookingNoShowCommand command, CancellationToken cancellationToken)
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
            decimal deducted = Math.Round(booking.DepositAmount * (booking.CancellationDeductionPercentage / 100m), 2);
            decimal refunded = booking.DepositAmount - deducted;
            string description = $"Facility booking no-show deposit settlement for booking {booking.Id}";

            List<LedgerLine> settlementLines =
            [
                new LedgerLine(LedgerAccountType.RefundableDepositsHeld, null, LedgerDirection.Debit, booking.DepositAmount, description),
            ];

            if (refunded > 0)
            {
                settlementLines.Add(new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, refunded, description));
            }

            if (deducted > 0)
            {
                settlementLines.Add(new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, deducted, description));
            }

            (LedgerPosting settlementPosting, IReadOnlyList<LedgerEntry> settlementEntries) = LedgerPosting.Create(
                tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description, "FacilityBookingNoShowSettlement",
                booking.Id.Value, settlementLines, nowUtc);

            ledgerPostings.Add(settlementPosting);
            ledgerEntries.AddRange(settlementEntries);

            refundedAmount = refunded;
            deductedAmount = deducted;
            settlementPostingId = settlementPosting.Id;
        }

        booking.MarkNoShow(refundedAmount, deductedAmount, settlementPostingId, nowUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Booking {BookingId} marked no-show, tenant {TenantId}", id, tenantId);

        return booking.ToDto();
    }
}
