using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Application.Features.Billing.Services;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Commands.AssessFine;

/// <summary>
/// Assesses a fine against a flat by reusing the Invoice/InvoiceLine/payment-allocation/void
/// machinery wholesale (<see cref="InvoiceSource.Fine"/>) rather than a parallel entity — same
/// rationale as Utility billing reusing it for Slice F. Structurally a one-line, one-off invoice:
/// sequence bump + invoice/line insert + ledger posting commit atomically, same pattern as
/// <c>BillReadingCommandHandler</c>.
/// </summary>
public sealed class AssessFineCommandHandler(
    IFlatRepository flats,
    IBuildingRepository buildings,
    IInvoiceRepository invoices,
    IInvoiceSequenceRepository sequences,
    IFinancialPostingService financialPosting,
    IResponsiblePartyResolver responsibleParties,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<AssessFineCommandHandler> logger
) : IRequestHandler<AssessFineCommand, InvoiceDto>
{
    public async ValueTask<InvoiceDto> Handle(AssessFineCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        Building building = await buildings.GetByIdAsync(flat.BuildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), flat.BuildingId.Value);

        await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        DateTimeOffset nowUtc = clock.UtcNow;
        DateOnly invoiceDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        int seq = await sequences.GetNextValueAsync(tenantId, flat.BuildingId, invoiceDate.Year, cancellationToken);
        string invoiceNumber = $"FIN-{building.Code}-{invoiceDate.Year}-{seq:D6}";

        InvoiceLineInput lineInput = new(
            null, "Fine", "Fine", "Fixed", command.Amount, null, 1m, command.Amount, command.Reason);

        FinancialPostingLine[] postingLines =
        [
            new FinancialPostingLine(LedgerAccountType.ResidentReceivable, flatId, LedgerDirection.Debit, command.Amount),
            new FinancialPostingLine(LedgerAccountType.FineIncome, null, LedgerDirection.Credit, command.Amount),
        ];

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, command.BusinessDate, $"Fine {invoiceNumber}: {command.Reason}", "FineAssessment", null, postingLines),
            cancellationToken);

        ResponsiblePartySnapshot? responsibleParty = await responsibleParties.ResolveAsync(
            tenantId, flatId, command.BusinessDate, cancellationToken);

        (Invoice invoice, IReadOnlyList<InvoiceLine> lines) = Invoice.Issue(
            tenantId, flat.BuildingId, flatId, invoiceNumber, InvoiceSource.Fine, command.BusinessDate,
            command.BusinessDate, invoiceDate, command.BusinessDate, [lineInput], posted.Posting.Id,
            LedgerAccountType.FineIncome, responsibleParty, nowUtc);

        invoices.Add(invoice);
        invoices.AddLines(lines);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Fine {InvoiceId} ('{InvoiceNumber}') assessed for flat {FlatId}, amount {Amount}, tenant {TenantId}",
            invoice.Id, invoiceNumber, flatId, command.Amount, tenantId);

        return invoice.ToDto();
    }
}
