using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Amenities.Commands.InspectBooking;

/// <summary>
/// If a deposit was collected at ConfirmPayment, posts the settlement ledger entry (refund portion to
/// CashOrBank, deduction portion to AssociationRevenue) before calling <see cref="Booking.Inspect"/> —
/// see plan §5. A single <see cref="IUnitOfWork.SaveChangesAsync"/> call is enough (no extra async
/// round-trip like a sequence generator is needed), so no explicit transaction is opened — same
/// simpler pattern as <c>VoidInvoiceCommandHandler</c>, not <c>BillReadingCommandHandler</c>.
/// </summary>
public sealed class InspectBookingCommandHandler(
    IBookingRepository bookings,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<InspectBookingCommandHandler> logger
) : IRequestHandler<InspectBookingCommand, BookingDto>
{
    public async ValueTask<BookingDto> Handle(InspectBookingCommand command, CancellationToken cancellationToken)
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

        decimal deducted = command.DamageDeductionAmount ?? 0m;
        if (deducted > booking.DepositAmount)
        {
            throw new ConflictException(
                $"DamageDeductionAmount ({deducted}) cannot exceed the held deposit ({booking.DepositAmount}).");
        }

        if (deducted > 0 && string.IsNullOrWhiteSpace(command.DamageDeductionReason))
        {
            throw new ConflictException("DamageDeductionReason is required when DamageDeductionAmount is positive.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;

        decimal? refundedAmount = null;
        decimal? deductedAmount = null;
        LedgerPostingId? settlementPostingId = null;

        if (booking.DepositAmount > 0)
        {
            decimal refunded = booking.DepositAmount - deducted;
            string description = $"Facility booking deposit settlement for booking {booking.Id}";

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
                tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description, "FacilityBookingDepositSettlement",
                booking.Id.Value, settlementLines, nowUtc);

            ledgerPostings.Add(settlementPosting);
            ledgerEntries.AddRange(settlementEntries);

            refundedAmount = refunded;
            deductedAmount = deducted;
            settlementPostingId = settlementPosting.Id;
        }

        booking.Inspect(
            currentUser.UserId, command.Notes, command.DamageDeductionReason, refundedAmount, deductedAmount,
            settlementPostingId, nowUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Booking {BookingId} inspected and closed, tenant {TenantId}", id, tenantId);

        return booking.ToDto();
    }
}
