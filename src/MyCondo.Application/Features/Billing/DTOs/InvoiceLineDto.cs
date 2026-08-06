namespace MyCondo.Application.Features.Billing.DTOs;

public sealed record InvoiceLineDto(
    Guid InvoiceLineId,
    Guid? ServiceChargeRuleId,
    string RuleNameSnapshot,
    string RuleCategorySnapshot,
    string CalculationMethodSnapshot,
    decimal RateSnapshot,
    decimal? AreaSqFtSnapshot,
    decimal Quantity,
    decimal LineAmount,
    string Description);
