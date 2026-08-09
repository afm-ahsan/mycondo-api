namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Reconciles the global permission catalogue (<c>identity.permissions</c> — no tenant_id/RLS, the
/// same set for every tenant) against <c>PermissionCatalogue.Entries</c>. Historically these rows were
/// seeded directly by EF Core migrations (<c>Seed_Permission_Catalogue</c> and its successors); those
/// migrations are preserved as historical record and are not re-run for new environments — this seeder
/// is now the single source that keeps the table in sync with the catalogue going forward. Additive
/// only: an existing row not in the catalogue is never touched or removed.
/// </summary>
public interface IPermissionSeeder
{
    /// <summary>
    /// Runs first in the seed orchestration order (<c>DatabaseSeederExtensions.SeedDatabaseAsync</c>) —
    /// every role/permission-grant seeder that follows resolves permission names against this table.
    /// Caller owns <c>IUnitOfWork.SaveChangesAsync</c> afterward.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken);
}
