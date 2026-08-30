using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.GenerateSubscriptionInvoice;

/// <summary>
/// Issues a <see cref="SubscriptionInvoice"/> from an organization's current subscription (ADR-034 Task
/// 14B) — deliberately as narrow as <see cref="Commands.ChangeOrganizationSubscription.ChangeOrganizationSubscriptionCommandHandler"/>:
/// no proration, scheduling, or subscription-lifecycle side effects.
///
/// <para><b>Commercial source of truth:</b> the invoice line is populated verbatim from the
/// subscription's own commercial snapshot (<see cref="OrganizationSubscription.BasePrice"/>/
/// <see cref="OrganizationSubscription.Discount"/>/<see cref="OrganizationSubscription.EffectivePrice"/>/
/// <see cref="OrganizationSubscription.Currency"/>) — the live <see cref="SubscriptionPackageVersion"/>
/// is read only for its name/version-number display provenance, never for pricing.</para>
///
/// <para><b>Billing period:</b> only <see cref="GenerateSubscriptionInvoiceCommand.BillingPeriodStart"/>
/// is caller-supplied; the end date is deterministically derived from the subscription's own
/// <see cref="OrganizationSubscription.BillingCycle"/> so a period can never be requested that doesn't
/// match what the subscription is actually billed for.</para>
///
/// <para><b>Idempotency:</b> an application-level pre-check against
/// <see cref="ISubscriptionInvoiceRepository.GetForOrganizationSubscriptionAsync"/> rejects an obvious
/// duplicate before any write; the Task 14A database unique index on (OrganizationSubscriptionId,
/// BillingPeriodStart, BillingPeriodEnd) remains the durable backstop for a concurrent race, translated
/// to <see cref="ConflictException"/> by the Infrastructure <c>MyCondoDbContext.SaveChangesAsync</c>
/// override rather than leaking a raw PostgreSQL unique-violation.</para>
///
/// <para><b>Due date:</b> no Platform due-date policy exists yet. Starts from the tenant Billing side's
/// established convention for periodic invoices (<c>GenerateInvoiceBatchCommandHandler</c>:
/// <c>DueDate = PeriodEnd</c>), clamped forward to <see cref="DateOnly"/> <c>IssueDate</c> so a
/// retroactive manual generation (the period has already ended by the time an admin gets around to
/// issuing it) never produces <c>DueDate &lt; IssueDate</c>, which <see cref="SubscriptionInvoice.Issue"/>
/// rejects outright.</para>
/// </summary>
public sealed class GenerateSubscriptionInvoiceCommandHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    ISubscriptionInvoiceRepository subscriptionInvoices,
    ISubscriptionPackageRepository subscriptionPackages,
    ISubscriptionPackageVersionRepository subscriptionPackageVersions,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<GenerateSubscriptionInvoiceCommandHandler> logger
) : IRequestHandler<GenerateSubscriptionInvoiceCommand, Guid>
{
    public async ValueTask<Guid> Handle(GenerateSubscriptionInvoiceCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        OrganizationSubscription subscription =
            await organizationSubscriptions.GetCurrentForTenantAsync(tenant.Id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(OrganizationSubscription), command.OrganizationId);

        // Only a commercially live subscription can be billed — mirrors the exact statuses
        // ChangePackageVersion/Cancel already allow. Restricted/Expired/Canceled subscriptions are past
        // the point of generating new charges; those lifecycle facts are recorded elsewhere and are never
        // inferred or altered here (ADR-034 Task 14B §"Subscription Eligibility").
        if (subscription.Status != OrganizationSubscriptionStatus.Active
            && subscription.Status != OrganizationSubscriptionStatus.PastDue)
        {
            throw new ConflictException(
                $"Subscription '{subscription.Id.Value}' is {subscription.Status} and cannot be invoiced.");
        }

        DateOnly billingPeriodEnd = subscription.BillingCycle switch
        {
            BillingCycle.Monthly => command.BillingPeriodStart.AddMonths(1).AddDays(-1),
            BillingCycle.Quarterly => command.BillingPeriodStart.AddMonths(3).AddDays(-1),
            BillingCycle.SemiAnnual => command.BillingPeriodStart.AddMonths(6).AddDays(-1),
            BillingCycle.Annual => command.BillingPeriodStart.AddYears(1).AddDays(-1),
            _ => throw new ArgumentOutOfRangeException(nameof(command), subscription.BillingCycle, "Unknown billing cycle.")
        };

        IReadOnlyList<SubscriptionInvoice> existingInvoices =
            await subscriptionInvoices.GetForOrganizationSubscriptionAsync(subscription.Id, cancellationToken);
        bool alreadyInvoiced = existingInvoices.Any(i =>
            i.BillingPeriodStart == command.BillingPeriodStart && i.BillingPeriodEnd == billingPeriodEnd);
        if (alreadyInvoiced)
        {
            throw new ConflictException(
                $"An invoice already exists for subscription '{subscription.Id.Value}' covering {command.BillingPeriodStart:yyyy-MM-dd}..{billingPeriodEnd:yyyy-MM-dd}.");
        }

        SubscriptionPackageVersion packageVersion = (await subscriptionPackageVersions.GetAllAsync(cancellationToken))
                .SingleOrDefault(v => v.Id == subscription.PackageVersionId)
            ?? throw new NotFoundException(nameof(SubscriptionPackageVersion), subscription.PackageVersionId.Value);

        SubscriptionPackage package = (await subscriptionPackages.GetAllAsync(cancellationToken))
                .SingleOrDefault(p => p.Id == packageVersion.PackageId)
            ?? throw new NotFoundException(nameof(SubscriptionPackage), packageVersion.PackageId.Value);

        DateTimeOffset nowUtc = clock.UtcNow;
        DateOnly issueDate = DateOnly.FromDateTime(DhakaTimeZone.ToLocal(nowUtc).DateTime);
        DateOnly dueDate = billingPeriodEnd > issueDate ? billingPeriodEnd : issueDate;

        string invoiceNumber = $"SUB-INV-{command.BillingPeriodStart:yyyyMM}-{subscription.Id.Value:N}";
        string description =
            $"{package.Name} ({subscription.BillingCycle}) subscription charge, {command.BillingPeriodStart:yyyy-MM-dd}..{billingPeriodEnd:yyyy-MM-dd}";

        SubscriptionInvoiceLineInput lineInput = new(
            packageVersion.Id, package.Name, packageVersion.Version, subscription.BillingCycle,
            subscription.BasePrice, subscription.Discount, subscription.EffectivePrice, description);

        (SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines) = SubscriptionInvoice.Issue(
            subscription.TenantId, subscription.Id, invoiceNumber, command.BillingPeriodStart, billingPeriodEnd,
            issueDate, dueDate, subscription.Currency, [lineInput], nowUtc);

        subscriptionInvoices.Add(invoice);
        subscriptionInvoices.AddLines(lines);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Generated subscription invoice {InvoiceId} ({InvoiceNumber}) for organization {TenantId} subscription {SubscriptionId}, period {PeriodStart}..{PeriodEnd}",
            invoice.Id, invoiceNumber, tenant.Id, subscription.Id, command.BillingPeriodStart, billingPeriodEnd);

        return invoice.Id.Value;
    }
}
