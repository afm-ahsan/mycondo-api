using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Billing.Commands.VoidInvoice;

public sealed class VoidInvoiceCommandHandler(
    IInvoiceRepository invoices,
    IFinancialPostingService financialPosting,
    IFinanceAuditLogRepository auditLog,
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

        // Reverse of the original issue posting (Debit <income account> / Credit ResidentReceivable).
        // Uses invoice.IncomeAccountType — the exact account credited at issuance — rather than
        // re-deriving it from Source, since Source alone can't distinguish (e.g.) Gas from Electricity
        // utility invoices. See Invoice.IncomeAccountType's doc comment.
        FinancialPostingLine[] reversingLines =
        [
            new FinancialPostingLine(invoice.IncomeAccountType, null, LedgerDirection.Debit, invoice.TotalAmount),
            new FinancialPostingLine(LedgerAccountType.ResidentReceivable, invoice.FlatId, LedgerDirection.Credit, invoice.TotalAmount),
        ];

        FinancialPostingResult reversal = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description, "InvoiceVoid",
                invoice.LedgerPostingId.Value, reversingLines, IsPrivilegedAdjustment: true),
            cancellationToken);

        // Throws InvoiceAlreadyVoidException / InvoiceCannotBeVoidedException before SaveChangesAsync
        // is reached — the reversal posting is already staged on the change tracker at this point, but
        // an unhandled exception here means SaveChangesAsync never runs, so nothing actually commits.
        invoice.Void(command.Reason, currentUser.UserId, reversal.Posting.Id, nowUtc);
        auditLog.Add(FinanceAuditLogEntry.Record(
            tenantId, nowUtc, currentUser.UserId, "Invoice.Void", nameof(Invoice), id.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { reason = command.Reason })));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Invoice {InvoiceId} voided for tenant {TenantId}, reversal posting {PostingId}",
            id, tenantId, reversal.Posting.Id);

        return invoice.ToDto();
    }
}
