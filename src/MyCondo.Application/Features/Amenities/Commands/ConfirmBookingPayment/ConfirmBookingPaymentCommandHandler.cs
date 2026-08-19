using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Amenities.Commands.ConfirmBookingPayment;

/// <summary>
/// Bills the booking-charge invoice (if any) and posts the deposit-collection ledger entry (if any) in
/// one transaction, reusing Slice E/F's transaction/sequence/ledger machinery verbatim — see plan §5.
/// </summary>
public sealed class ConfirmBookingPaymentCommandHandler(
    IBookingRepository bookings,
    IBuildingRepository buildings,
    IInvoiceRepository invoices,
    IInvoiceSequenceRepository sequences,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ConfirmBookingPaymentCommandHandler> logger
) : IRequestHandler<ConfirmBookingPaymentCommand, BookingDto>
{
    public async ValueTask<BookingDto> Handle(ConfirmBookingPaymentCommand command, CancellationToken cancellationToken)
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

        Building building = await buildings.GetByIdAsync(booking.BuildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), booking.BuildingId.Value);

        await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        DateTimeOffset nowUtc = clock.UtcNow;
        DateOnly invoiceDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        DateOnly bookingDate = DateOnly.FromDateTime(booking.StartAtUtc.UtcDateTime);

        InvoiceId? invoiceId = null;
        if (booking.BookingChargeAmount > 0)
        {
            int seq = await sequences.GetNextValueAsync(tenantId, booking.BuildingId, invoiceDate.Year, cancellationToken);
            string invoiceNumber = $"INV-{building.Code}-{invoiceDate.Year}-{seq:D6}";
            string description = $"Facility booking charge {invoiceNumber} for booking {booking.Id}";

            FinancialPostingLine[] chargeLines =
            [
                new FinancialPostingLine(LedgerAccountType.ResidentReceivable, booking.FlatId, LedgerDirection.Debit, booking.BookingChargeAmount),
                new FinancialPostingLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, booking.BookingChargeAmount),
            ];

            FinancialPostingResult chargePosted = await financialPosting.PostAsync(
                new FinancialPostingRequest(tenantId, invoiceDate, description, "FacilityBookingCharge", booking.Id.Value, chargeLines),
                cancellationToken);

            InvoiceLineInput lineInput = new(
                null, "Facility Booking Charge", "Amenities", "Fixed", booking.BookingChargeAmount, null, 1,
                booking.BookingChargeAmount, description);

            (Invoice invoice, IReadOnlyList<InvoiceLine> lines) = Invoice.Issue(
                tenantId, booking.BuildingId, booking.FlatId, invoiceNumber, InvoiceSource.FacilityBooking, bookingDate,
                bookingDate, invoiceDate, bookingDate, [lineInput], chargePosted.Posting.Id, nowUtc);

            invoices.Add(invoice);
            invoices.AddLines(lines);
            invoiceId = invoice.Id;
        }

        LedgerPostingId? depositPostingId = null;
        if (booking.DepositAmount > 0)
        {
            string description = $"Facility booking deposit held for booking {booking.Id}";

            FinancialPostingLine[] depositLines =
            [
                new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, booking.DepositAmount),
                new FinancialPostingLine(LedgerAccountType.RefundableDepositsHeld, null, LedgerDirection.Credit, booking.DepositAmount),
            ];

            FinancialPostingResult depositPosted = await financialPosting.PostAsync(
                new FinancialPostingRequest(tenantId, invoiceDate, description, "FacilityBookingDeposit", booking.Id.Value, depositLines),
                cancellationToken);

            depositPostingId = depositPosted.Posting.Id;
        }

        booking.ConfirmPayment(invoiceId, depositPostingId, nowUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Booking {BookingId} payment confirmed (invoice {InvoiceId}, deposit posting {DepositPostingId}), tenant {TenantId}",
            id, invoiceId, depositPostingId, tenantId);

        return booking.ToDto();
    }
}
