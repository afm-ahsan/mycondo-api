using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for expenses.expense_categories/expenses.expense_types/expenses.expenses
/// (Template 3 extends the pre-existing Phase 4 expense_types/expenses coverage with the new
/// expense_categories table) — same pattern as FlatOwnershipRlsTests. Requires a Docker daemon; not
/// executed in the environment this was authored in — see MultiTenancyPostgresFixture's doc comment.
/// </summary>
public class ExpensesRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public ExpensesRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExpenseCategories_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<ExpenseCategory>().Add(ExpenseCategory.Create(tenantA, "Utilities", "UTIL", null, 1, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<ExpenseCategory>().Add(ExpenseCategory.Create(tenantB, "Security", "SEC", null, 1, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<ExpenseCategory> visible = await asTenantA.Set<ExpenseCategory>().ToListAsync();
            visible.Should().ContainSingle(c => c.Code == "UTIL");
            visible.Should().NotContain(c => c.Code == "SEC");
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<ExpenseCategory> visible = await noTenant.Set<ExpenseCategory>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's expense categories");
        }
    }

    [Fact]
    public async Task ExpenseTypes_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        ExpenseCategoryId categoryA = ExpenseCategoryId.New();
        ExpenseCategoryId categoryB = ExpenseCategoryId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<ExpenseType>().Add(ExpenseType.Create(tenantA, categoryA, "Cleaning", "CLN", null, 1, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<ExpenseType>().Add(ExpenseType.Create(tenantB, categoryB, "Security", "SEC", null, 1, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<ExpenseType> visible = await asTenantA.Set<ExpenseType>().ToListAsync();
            visible.Should().ContainSingle(t => t.Code == "CLN");
            visible.Should().NotContain(t => t.Code == "SEC");
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<ExpenseType> visible = await noTenant.Set<ExpenseType>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's expense types");
        }
    }

    [Fact]
    public async Task Expenses_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        BuildingId buildingA = BuildingId.New();
        BuildingId buildingB = BuildingId.New();
        ExpenseTypeId expenseTypeA = ExpenseTypeId.New();
        ExpenseTypeId expenseTypeB = ExpenseTypeId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<Expense>().Add(Expense.Record(
                tenantA, buildingA, expenseTypeA, fundId: null, DateOnly.FromDateTime(DateTime.UtcNow),
                accountingDate: null, "Tenant A expense", null, null, 100m, isPaid: false, PaymentMethod.Cash,
                null, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<Expense>().Add(Expense.Record(
                tenantB, buildingB, expenseTypeB, fundId: null, DateOnly.FromDateTime(DateTime.UtcNow),
                accountingDate: null, "Tenant B expense", null, null, 200m, isPaid: false, PaymentMethod.Cash,
                null, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<Expense> visible = await asTenantA.Set<Expense>().ToListAsync();
            visible.Should().ContainSingle(e => e.BuildingId == buildingA);
            visible.Should().NotContain(e => e.BuildingId == buildingB);
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<Expense> visible = await noTenant.Set<Expense>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's expenses");
        }
    }

    [Fact]
    public async Task Association_Wide_Expense_With_No_Building_Is_Still_Tenant_Isolated()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        ExpenseTypeId expenseTypeA = ExpenseTypeId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<Expense>().Add(Expense.Record(
                tenantA, buildingId: null, expenseTypeA, fundId: null, DateOnly.FromDateTime(DateTime.UtcNow),
                accountingDate: null, "Association-wide expense", null, null, 500m, isPaid: false,
                PaymentMethod.Cash, null, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantB = _fixture.CreateDbContext(tenantB))
        {
            List<Expense> visible = await asTenantB.Set<Expense>().ToListAsync();
            visible.Should().BeEmpty("tenant B must never see tenant A's association-wide expense");
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<Expense> visible = await asTenantA.Set<Expense>().ToListAsync();
            visible.Should().ContainSingle(e => e.BuildingId == null && e.Description == "Association-wide expense");
        }
    }

    [Fact]
    public async Task Insert_Expense_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        // Row claims tenantA while the connection's context is tenantB — WITH CHECK must reject it.
        dbAsTenantB.Set<Expense>().Add(Expense.Record(
            tenantA, BuildingId.New(), ExpenseTypeId.New(), fundId: null, DateOnly.FromDateTime(DateTime.UtcNow),
            accountingDate: null, "Wrong tenant", null, null, 100m, isPaid: false, PaymentMethod.Cash, null,
            DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Theory]
    [InlineData("expenses.expense_categories")]
    [InlineData("expenses.expense_types")]
    [InlineData("expenses.expenses")]
    public async Task Expenses_Tables_Have_Rls_Enabled_And_Forced(string qualifiedTableName)
    {
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId: null);

        RlsFlags row = await db.Database
            .SqlQuery<RlsFlags>(
                $"""
                SELECT relrowsecurity AS row_security, relforcerowsecurity AS force_row_security
                FROM pg_class
                WHERE oid = {qualifiedTableName}::regclass
                """)
            .SingleAsync();

        row.RowSecurity.Should().BeTrue($"{qualifiedTableName} must have RLS enabled");
        row.ForceRowSecurity.Should().BeTrue($"{qualifiedTableName} must FORCE RLS (the app's DB role owns the table)");
    }

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
