namespace MyCondo.Infrastructure.Persistence.Seeding.Models;

/// <summary>
/// Development/test-only bootstrap dataset for the ARP local environment (mycondo-seed-data
/// -architecture-refactor-v2.md). These plaintext passwords are seed *inputs* only — see
/// <see cref="Extensions.UserSeedExtensions.EnsureUserAsync"/>, which hashes every one of them through
/// the same <c>IPasswordHasher</c> normal registration uses before anything reaches PostgreSQL. Never
/// referenced outside <see cref="Extensions.DevelopmentSeedExtensions"/>, which only runs when
/// <c>IHostEnvironment.IsDevelopment()</c> is true (see Program.cs) — never in production.
/// </summary>
internal static class DevelopmentBootstrapConstants
{
    public const string TenantName = "Akter Residence Park";
    public const string TenantSlug = "arp";

    public const string SuperAdminEmail = "sadmin@mycondo.com";
    public const string SuperAdminFullName = "SuperAdmin";
    public const string SuperAdminPassword = "SAdmin@1357#";

    public const string AdminEmail = "admin@mycondo.com";
    public const string AdminFullName = "Tenant Admin";
    public const string AdminPassword = "Admin@1357#";

    /// <summary>Existing domain convention for a tenant-level administrator — see
    /// <c>DefaultRoleCatalogueSeeder</c>. This codebase has no separate "Admin"/"TenantAdmin" role
    /// name; <c>BuildingAdmin</c> is the closest existing role to what the spec calls "Tenant Admin".</summary>
    public const string AdminRoleName = "BuildingAdmin";

    public const string TestUserEmail = "test@mycondo.com";
    public const string TestUserFullName = "TestOwner";
    public const string TestUserPassword = "Test@1357#";

    /// <summary>Existing domain convention for a low-privilege resident account — see
    /// <c>DefaultRoleCatalogueSeeder</c>'s <c>Owner</c> role (view-only + complaint creation).</summary>
    public const string TestUserRoleName = "Owner";
}
