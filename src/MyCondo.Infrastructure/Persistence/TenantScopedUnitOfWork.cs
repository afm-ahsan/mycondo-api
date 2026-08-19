using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.Infrastructure.Persistence;

public sealed class TenantScopedUnitOfWork(MyCondoDbContext db) : ITenantScopedUnitOfWork
{
    public ITenantRepository Tenants { get; } = new TenantRepository(db);
    public IUserRepository Users { get; } = new UserRepository(db);
    public IRoleRepository Roles { get; } = new RoleRepository(db);
    public IPermissionRepository Permissions { get; } = new PermissionRepository(db);
    public IRolePermissionRepository RolePermissions { get; } = new RolePermissionRepository(db);
    public IRoleAssignmentRepository RoleAssignments { get; } = new RoleAssignmentRepository(db);
    public ITenantModuleRepository TenantModules { get; } = new TenantModuleRepository(db);
    public IExpenseCategoryRepository ExpenseCategories { get; } = new ExpenseCategoryRepository(db);
    public IExpenseTypeRepository ExpenseTypes { get; } = new ExpenseTypeRepository(db);
    public IChartOfAccountRepository ChartOfAccounts { get; } = new ChartOfAccountRepository(db);
    public IAccountMappingRepository AccountMappings { get; } = new AccountMappingRepository(db);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => db.DisposeAsync();
}
