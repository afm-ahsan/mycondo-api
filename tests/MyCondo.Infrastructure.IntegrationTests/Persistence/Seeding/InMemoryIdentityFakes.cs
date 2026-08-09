using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.IntegrationTests.Persistence.Seeding;

/// <summary>
/// Minimal in-memory fakes of the repository interfaces, used only to prove
/// <c>DevelopmentSeedExtensions.SeedArpDevelopmentBootstrapAsync</c>'s idempotency end-to-end (run
/// twice, assert no duplicates) without a real PostgreSQL instance. Deliberately mimics EF Core's
/// real "queries hit the database, not the change tracker" behavior — <c>Add()</c> only stages an
/// item; queries only see it after <see cref="FakeUnitOfWork.SaveChangesAsync"/> flushes staged items
/// into the committed set. A same-request re-query of something just <c>Add()</c>-ed but not yet
/// flushed will not find it, exactly like the real <c>MyCondoDbContext</c> — this is what caught
/// SeedArpDevelopmentBootstrapAsync's original missing-intermediate-SaveChanges bug. Not a replacement
/// for the Testcontainers-backed suites (<c>MyCondo.MultiTenancyTests</c>) that validate real RLS
/// enforcement.
/// </summary>
internal interface IFlushable
{
    void Flush();
}

internal sealed class FakeTenantRepository : ITenantRepository, IFlushable
{
    private readonly List<Tenant> _staged = [];
    public List<Tenant> Tenants { get; } = [];

    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct) =>
        Task.FromResult(Tenants.FirstOrDefault(t => t.Id == id));

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Tenants.FirstOrDefault(t => t.Id.Value == id));

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Tenants.FirstOrDefault(t => t.Slug == slug));

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) =>
        Task.FromResult(Tenants.Any(t => t.Slug == slug));

    public Task<bool> AnyAsync(CancellationToken ct) => Task.FromResult(Tenants.Count > 0);

    public void Add(Tenant tenant) => _staged.Add(tenant);

    public void Flush()
    {
        Tenants.AddRange(_staged);
        _staged.Clear();
    }
}

internal sealed class FakeUserRepository : IUserRepository, IFlushable
{
    private readonly List<User> _staged = [];
    public List<User> Users { get; } = [];

    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.TenantId == tenantId && u.Email == email));

    public Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken ct) =>
        Task.FromResult(Users.Any(u => u.TenantId == tenantId && u.Email == email));

    public Task<bool> AnyForTenantAsync(Guid tenantId, CancellationToken ct) =>
        Task.FromResult(Users.Any(u => u.TenantId == tenantId));

    public Task<List<User>> GetAllForTenantAsync(Guid tenantId, CancellationToken ct) =>
        Task.FromResult(Users.Where(u => u.TenantId == tenantId).ToList());

    public void Add(User user) => _staged.Add(user);

    public void Flush()
    {
        Users.AddRange(_staged);
        _staged.Clear();
    }
}

internal sealed class FakeRoleRepository : IRoleRepository, IFlushable
{
    private readonly List<Role> _staged = [];
    public List<Role> Roles { get; } = [];

    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken ct) =>
        Task.FromResult(Roles.FirstOrDefault(r => r.Id == id));

    public Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken ct) =>
        Task.FromResult(Roles.FirstOrDefault(r => r.TenantId == tenantId && r.Name == name));

    public Task<List<Role>> GetAllForTenantAsync(Guid tenantId, CancellationToken ct) =>
        Task.FromResult(Roles.Where(r => r.TenantId == tenantId).ToList());

    public void Add(Role role) => _staged.Add(role);

    public void Flush()
    {
        Roles.AddRange(_staged);
        _staged.Clear();
    }
}

internal sealed class FakePermissionRepository : IPermissionRepository
{
    public List<Permission> Permissions { get; } = [];

    public Task<List<Permission>> GetAllAsync(CancellationToken ct) => Task.FromResult(Permissions.ToList());

    public Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken ct) =>
        Task.FromResult(Permissions.FirstOrDefault(p => p.Id == id));

    public Task<bool> ExistsAsync(PermissionId id, CancellationToken ct) =>
        Task.FromResult(Permissions.Any(p => p.Id == id));
}

internal sealed class FakeRolePermissionRepository : IRolePermissionRepository, IFlushable
{
    private readonly List<RolePermission> _staged = [];
    public List<RolePermission> Grants { get; } = [];

    public Task<bool> ExistsAsync(RoleId roleId, PermissionId permissionId, CancellationToken ct) =>
        Task.FromResult(Grants.Any(g => g.RoleId == roleId && g.PermissionId == permissionId));

    public Task<RolePermission?> GetAsync(RoleId roleId, PermissionId permissionId, CancellationToken ct) =>
        Task.FromResult(Grants.FirstOrDefault(g => g.RoleId == roleId && g.PermissionId == permissionId));

    public Task<List<RolePermission>> GetForRoleAsync(RoleId roleId, CancellationToken ct) =>
        Task.FromResult(Grants.Where(g => g.RoleId == roleId).ToList());

    public void Add(RolePermission rolePermission) => _staged.Add(rolePermission);

    public void Remove(RolePermission rolePermission) => Grants.Remove(rolePermission);

    public void Flush()
    {
        Grants.AddRange(_staged);
        _staged.Clear();
    }
}

internal sealed class FakeRoleAssignmentRepository : IRoleAssignmentRepository, IFlushable
{
    private readonly List<RoleAssignment> _staged = [];
    public List<RoleAssignment> Assignments { get; } = [];

    public Task<bool> ExistsAsync(Guid tenantId, UserId userId, RoleId roleId, Guid? buildingId, CancellationToken ct) =>
        Task.FromResult(Assignments.Any(a =>
            a.TenantId == tenantId && a.UserId == userId && a.RoleId == roleId && a.BuildingId == buildingId));

    public Task<RoleAssignment?> GetAsync(Guid tenantId, UserId userId, RoleId roleId, Guid? buildingId, CancellationToken ct) =>
        Task.FromResult(Assignments.FirstOrDefault(a =>
            a.TenantId == tenantId && a.UserId == userId && a.RoleId == roleId && a.BuildingId == buildingId));

    public Task<int> CountTenantWideHoldersAsync(Guid tenantId, RoleId roleId, CancellationToken ct) =>
        Task.FromResult(Assignments.Count(a =>
            a.TenantId == tenantId && a.RoleId == roleId && a.BuildingId == null));

    public Task<List<RoleAssignment>> GetForRoleAsync(Guid tenantId, RoleId roleId, CancellationToken ct) =>
        Task.FromResult(Assignments.Where(a => a.TenantId == tenantId && a.RoleId == roleId).ToList());

    public void Add(RoleAssignment roleAssignment) => _staged.Add(roleAssignment);

    public void Remove(RoleAssignment roleAssignment) => Assignments.Remove(roleAssignment);

    public void Flush()
    {
        Assignments.AddRange(_staged);
        _staged.Clear();
    }
}

internal sealed class FakeUnitOfWork(params IFlushable[] repositories) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (IFlushable repository in repositories)
        {
            repository.Flush();
        }

        return Task.FromResult(0);
    }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        throw new NotSupportedException("Not needed by the seeding tests.");
}

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow => now;
}
