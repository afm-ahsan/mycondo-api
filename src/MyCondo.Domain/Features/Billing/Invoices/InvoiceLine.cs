using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;

namespace MyCondo.Domain.Features.Billing.Invoices;

/// <summary>
/// One line of an <see cref="Invoice"/> — every value is a snapshot taken at generation time
/// (<see cref="Invoice.Issue"/>). A line never reads back through <see cref="ServiceChargeRuleId"/>
/// to the live rule, nor through the parent invoice's flat to its current <c>AreaSqFt</c> — a rule
/// ending or a flat's area changing after generation cannot alter an already-issued line. Only
/// created via <see cref="Invoice.Issue"/>; no public constructor bypasses that.
/// </summary>
public sealed class InvoiceLine : Entity<InvoiceLineId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public InvoiceId InvoiceId { get; private set; }
    public ServiceChargeRuleId? ServiceChargeRuleId { get; private set; }
    public string RuleNameSnapshot { get; private set; }
    public string RuleCategorySnapshot { get; private set; }
    public string CalculationMethodSnapshot { get; private set; }
    public decimal RateSnapshot { get; private set; }
    public decimal? AreaSqFtSnapshot { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal LineAmount { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private InvoiceLine()
    {
        RuleNameSnapshot = null!;
        RuleCategorySnapshot = null!;
        CalculationMethodSnapshot = null!;
        Description = null!;
    }

    internal InvoiceLine(
        InvoiceLineId id,
        Guid tenantId,
        InvoiceId invoiceId,
        InvoiceLineInput input,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        InvoiceId = invoiceId;
        ServiceChargeRuleId = input.ServiceChargeRuleId;
        RuleNameSnapshot = input.RuleNameSnapshot;
        RuleCategorySnapshot = input.RuleCategorySnapshot;
        CalculationMethodSnapshot = input.CalculationMethodSnapshot;
        RateSnapshot = input.RateSnapshot;
        AreaSqFtSnapshot = input.AreaSqFtSnapshot;
        Quantity = input.Quantity;
        LineAmount = input.LineAmount;
        Description = input.Description;
        CreatedAtUtc = nowUtc;
    }
}
