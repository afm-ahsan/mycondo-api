using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Billing.Commands.ReverseFine;

/// <summary>
/// Reverses (voids) a fine that was assessed in error — a fine-scoped wrapper around exactly
/// <c>VoidInvoiceCommandHandler</c>'s logic (same <see cref="Invoice.Void"/> domain guard: only
/// possible while <c>AmountPaid == 0 &amp;&amp; WaivedAmount == 0</c>), duplicated rather than
/// delegated to keep the <c>billing.fine.reverse</c> permission from doubling as a backdoor into
/// generic <c>billing.invoice.void</c> — this handler 404s on any invoice whose
/// <see cref="Invoice.Source"/> isn't <see cref="InvoiceSource.Fine"/> before the caller's
/// authorization grant would otherwise let it reach a non-fine invoice.
/// </summary>
public sealed class ReverseFineCommandHandler(
    IInvoiceRepository invoices,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ReverseFineCommandHandler> logger
) : IRequestHandler<ReverseFineCommand, InvoiceDto>
{
    public async ValueTask<InvoiceDto> Handle(ReverseFineCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        InvoiceId id = new(command.FineId);
        Invoice invoice = await invoices.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), command.FineId);
        if (invoice.TenantId != tenantId || invoice.Source != InvoiceSource.Fine)
        {
            throw new NotFoundException(nameof(Invoice), command.FineId);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        string description = $"Reversal of fine {invoice.InvoiceNumber}: {command.Reason}";

        FinancialPostingLine[] reversingLines =
        [
            new FinancialPostingLine(invoice.IncomeAccountType, null, LedgerDirection.Debit, invoice.TotalAmount),
            new FinancialPostingLine(LedgerAccountType.ResidentReceivable, invoice.FlatId, LedgerDirection.Credit, invoice.TotalAmount),
        ];

        FinancialPostingResult reversal = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description, "InvoiceVoid",
                invoice.LedgerPostingId.Value, reversingLines),
            cancellationToken);

        invoice.Void(command.Reason, currentUser.UserId, reversal.Posting.Id, nowUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fine {InvoiceId} reversed for tenant {TenantId}, reversal posting {PostingId}",
            id, tenantId, reversal.Posting.Id);

        return invoice.ToDto();
    }
}
