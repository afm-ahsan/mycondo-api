using MyCondo.Domain.Features.Billing.ServiceChargeRules;

namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>
/// Unpersisted input to <see cref="Invoice.Issue"/> — the caller (Application-layer
/// <c>ServiceChargeCalculator</c> for service-charge lines, <c>UtilityCalculator</c> for utility
/// lines) has already computed <see cref="LineAmount"/> from a rate source and the flat/meter's
/// current state; this record carries that computation's inputs so <see cref="Invoice.Issue"/> can
/// snapshot them onto the persisted <see cref="InvoiceLine"/> without re-deriving anything.
/// <see cref="CalculationMethodSnapshot"/> is a plain string (not the <see cref="ServiceChargeRules.CalculationMethod"/>
/// enum) precisely so it can honestly describe either rate source's own mechanism
/// (<c>FixedAmount</c>/<c>PerSquareFoot</c> for service charges, <c>Metered</c>/<c>Fixed</c> for
/// utilities) — forcing utility billing to shoehorn into the service-charge enum would mislabel it.
/// </summary>
public sealed record InvoiceLineInput(
    ServiceChargeRuleId? ServiceChargeRuleId,
    string RuleNameSnapshot,
    string RuleCategorySnapshot,
    string CalculationMethodSnapshot,
    decimal RateSnapshot,
    decimal? AreaSqFtSnapshot,
    decimal Quantity,
    decimal LineAmount,
    string Description);
