namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Marks a Mediator request as CondoBD platform-subscription billing-resolution access — the narrow
/// exception ADR-032 §5 carves out of the Restricted/Expired/Canceled read-only rule (e.g. a future "view
/// outstanding platform invoice" or "record subscription payment" request). Reserved extension point only
/// (ADR-032 Task 10 §11/§41): ADR-034 platform billing is not implemented yet, so no request implements
/// this interface today.
///
/// <para><b>Non-negotiable scope boundary (ADR-032 Task 10 §40):</b> this is exclusively for CondoBD
/// charging the organization for its platform subscription. Tenant Finance operations (resident invoicing,
/// service charges, payments, ledger postings) are never billing-resolution operations for this purpose —
/// see <c>MyCondo.Application.Features.Finance</c>/<c>Billing</c>/<c>Payments</c>, none of which implement
/// this interface.</para>
/// </summary>
public interface IBillingResolutionOperation;
