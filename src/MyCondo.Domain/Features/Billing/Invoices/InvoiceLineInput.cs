using MyCondo.Domain.Features.Billing.ServiceChargeRules;

namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>
/// Unpersisted input to <see cref="Invoice.Issue"/> — the caller (Application-layer
/// <c>ServiceChargeCalculator</c>) has already computed <see cref="LineAmount"/> from a rule and a
/// flat's current state; this record carries that computation's inputs so <see cref="Invoice.Issue"/>
/// can snapshot them onto the persisted <see cref="InvoiceLine"/> without re-deriving anything.
/// </summary>
public sealed record InvoiceLineInput(
    ServiceChargeRuleId? ServiceChargeRuleId,
    string RuleNameSnapshot,
    string RuleCategorySnapshot,
    CalculationMethod CalculationMethodSnapshot,
    decimal RateSnapshot,
    decimal? AreaSqFtSnapshot,
    decimal Quantity,
    decimal LineAmount,
    string Description);
