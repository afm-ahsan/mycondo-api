using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Mappings;

internal static class BillingMappings
{
    public static ServiceChargeRuleDto ToDto(this ServiceChargeRule rule) => new(
        rule.Id.Value, rule.BuildingId.Value, rule.Category, rule.Name, rule.CalculationMethod.ToString(),
        rule.Rate, rule.UnitTypeFilter?.ToString(), rule.Frequency.ToString(), rule.EffectiveFrom, rule.EffectiveTo,
        rule.IsActive);

    public static InvoiceDto ToDto(this Invoice invoice) => new(
        invoice.Id.Value, invoice.BuildingId.Value, invoice.FlatId.Value, invoice.InvoiceNumber,
        invoice.PeriodStart, invoice.PeriodEnd, invoice.InvoiceDate, invoice.DueDate, invoice.SubtotalAmount,
        invoice.TotalAmount, invoice.AmountPaid, invoice.Balance, invoice.Status.ToString(), invoice.IssuedAtUtc,
        invoice.VoidedAtUtc, invoice.VoidedBy, invoice.VoidReason);

    public static InvoiceLineDto ToDto(this InvoiceLine line) => new(
        line.Id.Value, line.ServiceChargeRuleId?.Value, line.RuleNameSnapshot, line.RuleCategorySnapshot,
        line.CalculationMethodSnapshot.ToString(), line.RateSnapshot, line.AreaSqFtSnapshot, line.Quantity,
        line.LineAmount, line.Description);

    public static FlatMissingAreaDto ToMissingAreaDto(this Flat flat) => new(flat.Id.Value, flat.FlatNumber);
}
