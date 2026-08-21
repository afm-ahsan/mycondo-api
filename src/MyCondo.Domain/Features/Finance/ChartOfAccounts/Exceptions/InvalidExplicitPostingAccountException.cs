using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.ChartOfAccounts.Exceptions;

/// <summary>Thrown by the centralized posting service when a <c>FinancialPostingLine.ExplicitAccountId</c>
/// (Template 4's per-Financial-Account posting escape hatch) does not resolve to a chart-of-account record
/// that is safe to post against directly — missing, belonging to another tenant, or inactive. Mirrors
/// <see cref="AccountMappings.Exceptions.MissingAccountMappingException"/>'s "fail explicitly rather than
/// posting to a wrong/unsafe account" rule (ADR-027).</summary>
public sealed class InvalidExplicitPostingAccountException(Guid tenantId, ChartOfAccountId accountId, string reason)
    : DomainException($"Tenant {tenantId} cannot post against chart of account {accountId}: {reason}")
{
    public Guid TenantId { get; } = tenantId;
    public ChartOfAccountId AccountId { get; } = accountId;
}
