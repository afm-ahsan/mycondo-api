namespace MyCondo.Domain.Features.Billing.ServiceChargeRules;

/// <summary>
/// Only two calculation mechanisms exist. kickoff.md's module 5 note ("fixed / sqft / unit")
/// listed three, but "per unit" and "fixed amount" are the same mechanism — a rule scoped to one
/// unit type via <see cref="ServiceChargeRule.UnitTypeFilter"/> already produces a per-unit-type
/// fixed charge without a dedicated third method. Decided 2026-08-06, confirmed with the
/// client-side stakeholder via AskUserQuestion during Slice E planning. String-backed
/// (<c>HasConversion&lt;string&gt;()</c> in <c>ServiceChargeRuleConfiguration</c>) so a genuinely
/// new mechanism (e.g. a tiered/banded rate) can be added later as a pure enum + calculator-switch
/// addition — no schema change required.
/// </summary>
public enum CalculationMethod
{
    FixedAmount = 0,
    PerSquareFoot = 1,
}
