using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Commands.CorrectReading;

/// <summary>
/// Marks the original reading Corrected (valid from Finalized or Billed — see
/// <see cref="Reading.MarkCorrected"/>); if it was Billed, voids the associated invoice inline (same
/// reversing-ledger-posting logic as <c>VoidInvoiceCommandHandler</c>, run here rather than dispatched
/// as a separate command since it must commit in the same unit of work). Then records a brand-new
/// Draft reading referencing the original via <see cref="Reading.CorrectsReadingId"/> — the caller
/// must independently walk it through Review → Finalize → Bill again. See plan §9.
/// </summary>
public sealed class CorrectReadingCommandHandler(
    IReadingRepository readings,
    IInvoiceRepository invoices,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CorrectReadingCommandHandler> logger
) : IRequestHandler<CorrectReadingCommand, ReadingDto>
{
    public async ValueTask<ReadingDto> Handle(CorrectReadingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ReadingId originalId = new(command.ReadingId);
        Reading original = await readings.GetByIdAsync(originalId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reading), command.ReadingId);
        if (original.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Reading), command.ReadingId);
        }

        bool wasBilled = original.Status == ReadingStatus.Billed;
        InvoiceId? billedInvoiceId = original.InvoiceId;

        DateTimeOffset nowUtc = clock.UtcNow;

        // Throws ReadingInvalidTransitionException if not Finalized/Billed — before anything below
        // is staged.
        original.MarkCorrected(nowUtc);

        if (wasBilled)
        {
            Invoice invoice = await invoices.GetByIdAsync(billedInvoiceId!.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Invoice), billedInvoiceId.Value.Value);

            string voidDescription = $"Void of invoice {invoice.InvoiceNumber} (reading correction): {command.Reason}";
            LedgerLine[] reversingLines =
            [
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Debit, invoice.TotalAmount, voidDescription),
                new LedgerLine(LedgerAccountType.ResidentReceivable, invoice.FlatId, LedgerDirection.Credit, invoice.TotalAmount, voidDescription),
            ];

            (LedgerPosting reversingPosting, IReadOnlyList<LedgerEntry> reversingEntries) = LedgerPosting.Create(
                tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), voidDescription, "InvoiceVoid",
                invoice.LedgerPostingId.Value, reversingLines, nowUtc);

            invoice.Void(command.Reason, currentUser.UserId, reversingPosting.Id, nowUtc);

            ledgerPostings.Add(reversingPosting);
            ledgerEntries.AddRange(reversingEntries);
        }

        Reading correction = Reading.Record(
            tenantId, original.MeterId, original.FlatId, original.UtilityType, original.BuildingId,
            original.PeriodStart, original.PeriodEnd, command.PreviousReading, command.PresentReading,
            command.ReadingDate, command.OverrideReason, original.Id, nowUtc);

        readings.Add(correction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reading {OriginalReadingId} corrected by {CorrectionReadingId} (was billed: {WasBilled}), tenant {TenantId}",
            originalId, correction.Id, wasBilled, tenantId);

        return correction.ToDto();
    }
}
