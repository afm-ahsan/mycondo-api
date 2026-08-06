using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Billing.Commands.VoidInvoice;

public sealed class VoidInvoiceCommandHandler(
    IInvoiceRepository invoices,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<VoidInvoiceCommandHandler> logger
) : IRequestHandler<VoidInvoiceCommand, InvoiceDto>
{
    public async ValueTask<InvoiceDto> Handle(VoidInvoiceCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        InvoiceId id = new(command.InvoiceId);
        Invoice invoice = await invoices.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), command.InvoiceId);
        if (invoice.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Invoice), command.InvoiceId);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        string description = $"Void of invoice {invoice.InvoiceNumber}: {command.Reason}";

        // Reverse of the original issue posting (Debit ResidentReceivable / Credit AssociationRevenue).
        LedgerLine[] reversingLines =
        [
            new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Debit, invoice.TotalAmount, description),
            new LedgerLine(LedgerAccountType.ResidentReceivable, invoice.FlatId, LedgerDirection.Credit, invoice.TotalAmount, description),
        ];

        (LedgerPosting reversingPosting, IReadOnlyList<LedgerEntry> reversingEntries) = LedgerPosting.Create(
            tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description, "InvoiceVoid", invoice.LedgerPostingId.Value,
            reversingLines, nowUtc);

        // Throws InvoiceAlreadyVoidException / InvoiceCannotBeVoidedException before anything below
        // is staged — nothing is added to the change tracker unless this succeeds.
        invoice.Void(command.Reason, currentUser.UserId, reversingPosting.Id, nowUtc);

        ledgerPostings.Add(reversingPosting);
        ledgerEntries.AddRange(reversingEntries);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Invoice {InvoiceId} voided for tenant {TenantId}, reversal posting {PostingId}",
            id, tenantId, reversingPosting.Id);

        return invoice.ToDto();
    }
}
