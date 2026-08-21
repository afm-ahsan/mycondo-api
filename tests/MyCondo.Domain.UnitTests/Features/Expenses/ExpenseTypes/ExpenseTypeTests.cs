using AwesomeAssertions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Domain.UnitTests.Features.Expenses.ExpenseTypes;

public class ExpenseTypeTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly ExpenseCategoryId ExpenseCategoryId = ExpenseCategoryId.New();

    [Fact]
    public void Create_Trims_Name_And_Uppercases_Code()
    {
        ExpenseType expenseType = ExpenseType.Create(
            TenantId, ExpenseCategoryId, "  Cleaning  ", " clean ", " routine ", 1, Now);

        expenseType.Name.Should().Be("Cleaning");
        expenseType.Code.Should().Be("CLEAN");
        expenseType.Description.Should().Be("routine");
        expenseType.IsActive.Should().BeTrue();
        expenseType.ExpenseCategoryId.Should().Be(ExpenseCategoryId);
    }

    [Fact]
    public void Deactivate_Then_Activate_Round_Trips_And_Bumps_Version_Each_Time()
    {
        ExpenseType expenseType = ExpenseType.Create(TenantId, ExpenseCategoryId, "Cleaning", "CLN", null, 1, Now);

        expenseType.Deactivate(DateTimeOffset.UtcNow);
        expenseType.IsActive.Should().BeFalse();
        expenseType.Version.Should().Be(2);

        expenseType.Activate(DateTimeOffset.UtcNow);
        expenseType.IsActive.Should().BeTrue();
        expenseType.Version.Should().Be(3);
    }

    [Fact]
    public void Deactivate_Is_Idempotent()
    {
        ExpenseType expenseType = ExpenseType.Create(TenantId, ExpenseCategoryId, "Cleaning", "CLN", null, 1, Now);
        expenseType.Deactivate(DateTimeOffset.UtcNow);
        int versionAfterFirstDeactivate = expenseType.Version;

        expenseType.Deactivate(DateTimeOffset.UtcNow);

        expenseType.Version.Should().Be(versionAfterFirstDeactivate);
    }

    [Fact]
    public void Update_Changes_Category_Name_Code_Description_DisplayOrder()
    {
        ExpenseType expenseType = ExpenseType.Create(TenantId, ExpenseCategoryId, "Cleaning", "CLN", null, 1, Now);
        ExpenseCategoryId newCategoryId = ExpenseCategoryId.New();

        expenseType.Update(newCategoryId, "Security", "SEC", "Guard services", 2, DateTimeOffset.UtcNow);

        expenseType.ExpenseCategoryId.Should().Be(newCategoryId);
        expenseType.Name.Should().Be("Security");
        expenseType.Code.Should().Be("SEC");
        expenseType.Description.Should().Be("Guard services");
        expenseType.DisplayOrder.Should().Be(2);
        expenseType.Version.Should().Be(2);
    }

    [Fact]
    public void BackfillExpenseCategory_Sets_Category_Only_When_Currently_Null_Without_Bumping_Version()
    {
        ExpenseType expenseType = ExpenseType.Create(TenantId, ExpenseCategoryId, "Cleaning", "CLN", null, 1, Now);
        // Simulate a pre-Template-3 row by reflecting through Update to null isn't possible (Update
        // requires a category) — BackfillExpenseCategory itself is a no-op once a category is already
        // set, which is the behaviour under test here.
        int versionBeforeBackfill = expenseType.Version;

        expenseType.BackfillExpenseCategory(ExpenseCategoryId.New());

        expenseType.ExpenseCategoryId.Should().Be(ExpenseCategoryId);
        expenseType.Version.Should().Be(versionBeforeBackfill);
    }
}
