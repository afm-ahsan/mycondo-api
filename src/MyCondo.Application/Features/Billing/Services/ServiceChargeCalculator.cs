using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Services;

/// <summary>
/// Computes one <see cref="InvoiceLineInput"/> from a <see cref="ServiceChargeRule"/> and a
/// <see cref="Flat"/>'s current state — an Application-layer concern (it reads <see cref="Flat.AreaSqFt"/>)
/// rather than a domain invariant. Never zero-fills a missing input; returns a skip reason instead so
/// the caller can record it rather than generate an incorrect invoice line. Rounds to 2 decimal
/// places, away from zero — the chosen rounding mode for all Slice E currency arithmetic.
/// </summary>
public static class ServiceChargeCalculator
{
    public static ServiceChargeCalculationResult Calculate(ServiceChargeRule rule, Flat flat)
    {
        switch (rule.CalculationMethod)
        {
            case CalculationMethod.FixedAmount:
                return ServiceChargeCalculationResult.Ok(new InvoiceLineInput(
                    rule.Id, rule.Name, rule.Category, rule.CalculationMethod.ToString(), rule.Rate, null, 1m, rule.Rate,
                    $"{rule.Name} ({rule.Category})"));

            case CalculationMethod.PerSquareFoot:
                if (flat.AreaSqFt is not decimal areaSqFt)
                {
                    return ServiceChargeCalculationResult.Skip(
                        "Flat is missing AreaSqFt, required for a PerSquareFoot rule");
                }

                decimal amount = Math.Round(rule.Rate * areaSqFt, 2, MidpointRounding.AwayFromZero);
                return ServiceChargeCalculationResult.Ok(new InvoiceLineInput(
                    rule.Id, rule.Name, rule.Category, rule.CalculationMethod.ToString(), rule.Rate, areaSqFt, 1m, amount,
                    $"{rule.Name} ({rule.Category}): {areaSqFt:0.##} sqft @ {rule.Rate:0.00}/sqft"));

            default:
                throw new NotSupportedException($"Unhandled calculation method: {rule.CalculationMethod}.");
        }
    }
}

public readonly struct ServiceChargeCalculationResult
{
    public bool IsSuccess { get; }
    public InvoiceLineInput? Line { get; }
    public string? SkipReason { get; }

    private ServiceChargeCalculationResult(bool isSuccess, InvoiceLineInput? line, string? skipReason)
    {
        IsSuccess = isSuccess;
        Line = line;
        SkipReason = skipReason;
    }

    public static ServiceChargeCalculationResult Ok(InvoiceLineInput line) => new(true, line, null);

    public static ServiceChargeCalculationResult Skip(string reason) => new(false, null, reason);
}
