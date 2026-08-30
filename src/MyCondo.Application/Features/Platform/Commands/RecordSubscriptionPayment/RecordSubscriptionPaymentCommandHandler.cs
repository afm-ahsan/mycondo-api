using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.RecordSubscriptionPayment;

/// <summary>
/// Records a manual payment against a <see cref="SubscriptionInvoice"/> and applies it in the same unit
/// of work (ADR-034 Task 14C) — the invoice's <see cref="SubscriptionInvoice.OutstandingAmount"/>/
/// <see cref="SubscriptionInvoice.Status"/> change and the new <see cref="SubscriptionPayment"/> row are
/// both written by the one <see cref="IUnitOfWork.SaveChangesAsync"/> call below, so a failure leaves
/// neither half persisted (no separate transaction needed — a single EF Core <c>SaveChanges</c> call is
/// already atomic, same reasoning as <see cref="Commands.GenerateSubscriptionInvoice.GenerateSubscriptionInvoiceCommandHandler"/>).
///
/// <para><b>Idempotency:</b> an application-level pre-check against
/// <see cref="ISubscriptionPaymentRepository.ExistsByReferenceAsync"/> rejects an obvious duplicate
/// reference before any write; the database unique index on (TenantId, ReferenceNumber) remains the
/// durable backstop for a concurrent race, translated to <see cref="ConflictException"/> by the
/// Infrastructure <c>MyCondoDbContext.SaveChangesAsync</c> override — the same Task 14B pattern extended
/// to this constraint.</para>
///
/// <para><b>No tenant Finance:</b> this handler never touches tenant <c>Payment</c>/<c>Invoice</c>/
/// <c>LedgerPosting</c>/<c>ResidentAccount</c> — Platform SaaS billing and tenant condominium Finance
/// stay structurally separate (ADR-034 §1).</para>
/// </summary>
public sealed class RecordSubscriptionPaymentCommandHandler(
    ITenantRepository tenants,
    ISubscriptionInvoiceRepository subscriptionInvoices,
    ISubscriptionPaymentRepository subscriptionPayments,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RecordSubscriptionPaymentCommandHandler> logger
) : IRequestHandler<RecordSubscriptionPaymentCommand, Guid>
{
    public async ValueTask<Guid> Handle(RecordSubscriptionPaymentCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        SubscriptionInvoiceId invoiceId = new(command.SubscriptionInvoiceId);
        SubscriptionInvoice invoice = await subscriptionInvoices.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(SubscriptionInvoice), command.SubscriptionInvoiceId);

        if (invoice.TenantId != tenant.Id.Value)
        {
            throw new NotFoundException(nameof(SubscriptionInvoice), command.SubscriptionInvoiceId);
        }

        bool duplicateReference = await subscriptionPayments.ExistsByReferenceAsync(
            tenant.Id.Value, command.ReferenceNumber, cancellationToken);
        if (duplicateReference)
        {
            throw new ConflictException(
                $"A subscription payment with reference '{command.ReferenceNumber}' has already been recorded for organization '{tenant.Id.Value}'.");
        }

        DateTimeOffset nowUtc = clock.UtcNow;

        // Invoice is EF-tracked from GetByIdAsync, so this mutation is picked up by the same
        // SaveChangesAsync call below as the new SubscriptionPayment row — no explicit Update() call.
        invoice.ApplyPayment(command.Amount, command.Currency, nowUtc);

        SubscriptionPayment payment = SubscriptionPayment.Record(
            tenant.Id.Value, invoice.Id, command.Amount, command.Currency, command.PaymentDate,
            command.ReferenceNumber, command.Notes, nowUtc);

        subscriptionPayments.Add(payment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Recorded subscription payment {PaymentId} of {Amount} {Currency} against invoice {InvoiceId} for organization {TenantId}, outstanding now {Outstanding}, status {Status}",
            payment.Id, command.Amount, command.Currency, invoice.Id, tenant.Id, invoice.OutstandingAmount, invoice.Status);

        return payment.Id.Value;
    }
}
