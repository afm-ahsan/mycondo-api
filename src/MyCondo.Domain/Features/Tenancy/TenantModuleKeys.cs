namespace MyCondo.Domain.Features.Tenancy;

/// <summary>
/// The fixed catalogue of product modules a Platform SuperAdmin can enable/disable per organization.
/// Tenancy/Identity are always-on foundation concerns and are deliberately not represented here.
/// Mirrors the module list in mycondo-web/CLAUDE.md's feature-layout table.
/// </summary>
public static class TenantModuleKeys
{
    public static readonly string[] All =
    [
        "property", "billing", "payments", "expenses", "vendors", "payroll", "complaints",
        "notifications", "documents", "reporting", "security", "leasing", "residents",
        "utilities", "amenities", "maintenance", "operations",
    ];

    public static bool IsKnown(string moduleKey) => All.Contains(moduleKey, StringComparer.Ordinal);
}
