namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Seeds a tenant's default operational expense category catalogue (Cleaning, Security, Generator
/// Fuel, etc. — mycondo-docs expense-management task requirements) so a newly bootstrapped tenant has
/// a practical starting set instead of an empty picker. Reconciled by <c>Code</c>, the expense-type
/// equivalent of <c>IRoleRepository</c>'s natural-key catalogue reconciliation: missing default entries
/// are added, existing ones (even if the tenant renamed/deactivated them) are left untouched.
/// </summary>
public interface IExpenseTypeCatalogueSeeder
{
    /// <summary>
    /// Runs alongside the role-catalogue seeders at tenant-bootstrap time (first user registration,
    /// organization provisioning, <c>MyCondo.DbMigrator</c>'s bootstrap command) — same caller owns
    /// <c>SaveChangesAsync</c> afterward.
    /// </summary>
    Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
