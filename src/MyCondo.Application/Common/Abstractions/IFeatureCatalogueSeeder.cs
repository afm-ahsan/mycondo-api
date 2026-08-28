namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Reconciles the global Feature Catalogue (<c>platform.feature_definitions</c>/
/// <c>platform.feature_permissions</c> — no tenant_id/RLS, the same set for every tenant) against
/// <c>FeatureCatalogue.Entries</c>/<c>FeatureCatalogue.PermissionMappings</c> (ADR-033 §3/§5). Additive
/// only: an existing row not in the catalogue is never touched or removed — mirrors
/// <see cref="IPermissionSeeder"/>'s own established convention for a global reference catalogue.
/// </summary>
public interface IFeatureCatalogueSeeder
{
    /// <summary>
    /// Runs alongside the permission catalogue seeder in the seed orchestration order
    /// (<c>DatabaseSeederExtensions.SeedDatabaseAsync</c>) — every environment, every startup. Caller
    /// owns <c>IUnitOfWork.SaveChangesAsync</c> afterward.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken);
}
