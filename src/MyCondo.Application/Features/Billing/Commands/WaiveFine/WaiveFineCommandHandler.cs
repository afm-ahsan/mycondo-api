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

namespace MyCondo.Application.Features.Billing.Commands.WaiveFine;

/// <summary>
/// Writes off part or all of a fine's outstanding balance with no cash changing hands — Dr
/// AdjustmentsAndWaivers / Cr ResidentReceivable, the same waiver account the Finance foundation
/// (ADR-027) already provisioned but that no call site used until now. Restricted to
/// <see cref="InvoiceSource.Fine"/> invoices — waiving a Service Charge/Utility invoice isn't wired up
/// (out of this template's scope), enforced here rather than in <see cref="Invoice.Waive"/> itself,
/// which stays source-agnostic like every other Invoice domain method. One-time per fine — see
/// <see cref="Invoice.Waive"/>'s doc comment; the ledger posting's SourceId is the fine's own
/// InvoiceId, so a genuine duplicate request is rejected by the posting's idempotency check before
/// the domain guard is even reached.
/// </summary>
public sealed class WaiveFineCommandHandler(
    IInvoiceRepository invoices,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<WaiveFineCommandHandler> logger
) : IRequestHandler<WaiveFineCommand, InvoiceDto>
{
    public async ValueTask<InvoiceDto> Handle(WaiveFineCommand command, CancellationToken cancellationToken)
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
        string description = $"Waiver of fine {invoice.InvoiceNumber}: {command.Reason}";

        FinancialPostingLine[] waiverLines =
        [
            new FinancialPostingLine(LedgerAccountType.AdjustmentsAndWaivers, null, LedgerDirection.Debit, command.Amount),
            new FinancialPostingLine(LedgerAccountType.ResidentReceivable, invoice.FlatId, LedgerDirection.Credit, command.Amount),
        ];

        FinancialPostingResult waived = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, DateOnly.FromDateTime(nowUtc.UtcDateTime), description, "FineWaiver",
                invoice.Id.Value, waiverLines),
            cancellationToken);

        // Throws InvoiceAlreadyVoidException / InvoiceAlreadyWaivedException / ArgumentOutOfRangeException
        // before SaveChangesAsync — the waiver posting is already staged but nothing commits if this throws.
        invoice.Waive(command.Amount, command.Reason, currentUser.UserId, waived.Posting.Id, nowUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fine {InvoiceId} waived {Amount} for tenant {TenantId}, waiver posting {PostingId}",
            id, command.Amount, tenantId, waived.Posting.Id);

        return invoice.ToDto();
    }
}
