using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Application.Features.Billing.Services;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Application.Features.Utilities.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Commands.BillReading;

/// <summary>
/// Reuses Slice E's transaction/sequence/ledger machinery verbatim — see plan §7. One reading billed
/// per call; there is no utility batch-bill-all endpoint in this slice (readings are billed
/// individually after finalization).
/// </summary>
public sealed class BillReadingCommandHandler(
    IReadingRepository readings,
    IRatePlanRepository ratePlans,
    IBuildingRepository buildings,
    IInvoiceRepository invoices,
    IInvoiceSequenceRepository sequences,
    IFinancialPostingService financialPosting,
    IResponsiblePartyResolver responsibleParties,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<BillReadingCommandHandler> logger
) : IRequestHandler<BillReadingCommand, InvoiceDto>
{
    public async ValueTask<InvoiceDto> Handle(BillReadingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ReadingId readingId = new(command.ReadingId);
        Reading reading = await readings.GetByIdAsync(readingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reading), command.ReadingId);
        if (reading.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Reading), command.ReadingId);
        }

        RatePlan ratePlan = await ratePlans.GetApplicablePlanAsync(
            tenantId, reading.BuildingId, reading.UtilityType, reading.PeriodStart, reading.PeriodEnd, cancellationToken)
            ?? throw new ConflictException(
                $"No effective {reading.UtilityType} rate plan covers the period {reading.PeriodStart}..{reading.PeriodEnd} for this building.");

        IReadOnlyList<RateSlab> slabs = await ratePlans.GetSlabsForPlanAsync(ratePlan.Id, cancellationToken);

        Building building = await buildings.GetByIdAsync(reading.BuildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), reading.BuildingId.Value);

        InvoiceLineInput lineInput = UtilityCalculator.Calculate(ratePlan, slabs, reading);

        await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        DateTimeOffset nowUtc = clock.UtcNow;
        DateOnly invoiceDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        DateOnly dueDate = reading.PeriodEnd;

        int seq = await sequences.GetNextValueAsync(tenantId, reading.BuildingId, invoiceDate.Year, cancellationToken);
        string invoiceNumber = $"INV-{building.Code}-{invoiceDate.Year}-{seq:D6}";

        string description = $"Utility invoice {invoiceNumber} ({reading.UtilityType}) for flat, period {reading.PeriodStart}..{reading.PeriodEnd}";

        // Gas is differentiated per the Billing↔Finance integration's explicit scope; other utility
        // types (e.g. Electricity) keep posting to the generic AssociationRevenue account.
        LedgerAccountType incomeAccountType = reading.UtilityType == UtilityType.Gas
            ? LedgerAccountType.GasRecoveryIncome
            : LedgerAccountType.AssociationRevenue;

        FinancialPostingLine[] postingLines =
        [
            new FinancialPostingLine(LedgerAccountType.ResidentReceivable, reading.FlatId, LedgerDirection.Debit, lineInput.LineAmount),
            new FinancialPostingLine(incomeAccountType, null, LedgerDirection.Credit, lineInput.LineAmount),
        ];

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(tenantId, invoiceDate, description, "UtilityBill", null, postingLines),
            cancellationToken);

        ResponsiblePartySnapshot? responsibleParty = await responsibleParties.ResolveAsync(
            tenantId, reading.FlatId, invoiceDate, cancellationToken);

        (Invoice invoice, IReadOnlyList<InvoiceLine> lines) = Invoice.Issue(
            tenantId, reading.BuildingId, reading.FlatId, invoiceNumber, InvoiceSource.Utility, reading.PeriodStart,
            reading.PeriodEnd, invoiceDate, dueDate, [lineInput], posted.Posting.Id, incomeAccountType,
            responsibleParty, nowUtc);

        invoices.Add(invoice);
        invoices.AddLines(lines);

        reading.MarkBilled(invoice.Id, currentUser.UserId, nowUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Reading {ReadingId} billed as invoice {InvoiceId} ('{InvoiceNumber}'), tenant {TenantId}",
            readingId, invoice.Id, invoiceNumber, tenantId);

        return invoice.ToDto();
    }
}
