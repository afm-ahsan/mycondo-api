namespace MyCondo.Application.Common.Abstractions;

/// <summary>Seeds a new tenant's Chart of Accounts and Account Mappings — see
/// <c>MyCondo.Application.Common.Services.FinanceChartOfAccountSeeder</c>. Without this, every
/// existing Billing/Payments/Amenities/Utilities posting call site would fail immediately with
/// <c>MissingAccountMappingException</c> on a brand-new tenant (ADR-027).</summary>
public interface IFinanceChartOfAccountSeeder
{
    Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
