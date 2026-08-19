using AwesomeAssertions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;

namespace MyCondo.Domain.UnitTests.Features.Expenses.ExpenseCategories;

public class ExpenseCategoryTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Trims_Name_And_Uppercases_Code()
    {
        ExpenseCategory category = ExpenseCategory.Create(TenantId, "  Utilities  ", " util ", " power/water ", 1, Now);

        category.Name.Should().Be("Utilities");
        category.Code.Should().Be("UTIL");
        category.Description.Should().Be("power/water");
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Throws_For_Empty_TenantId()
    {
        Action act = () => ExpenseCategory.Create(Guid.Empty, "Utilities", "UTIL", null, 1, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_Then_Activate_Round_Trips_And_Bumps_Version_Each_Time()
    {
        ExpenseCategory category = ExpenseCategory.Create(TenantId, "Utilities", "UTIL", null, 1, Now);

        category.Deactivate(DateTimeOffset.UtcNow);
        category.IsActive.Should().BeFalse();
        category.Version.Should().Be(2);

        category.Activate(DateTimeOffset.UtcNow);
        category.IsActive.Should().BeTrue();
        category.Version.Should().Be(3);
    }

    [Fact]
    public void Deactivate_Is_Idempotent()
    {
        ExpenseCategory category = ExpenseCategory.Create(TenantId, "Utilities", "UTIL", null, 1, Now);
        category.Deactivate(DateTimeOffset.UtcNow);
        int versionAfterFirstDeactivate = category.Version;

        category.Deactivate(DateTimeOffset.UtcNow);

        category.Version.Should().Be(versionAfterFirstDeactivate);
    }

    [Fact]
    public void Update_Changes_Name_Code_Description_DisplayOrder()
    {
        ExpenseCategory category = ExpenseCategory.Create(TenantId, "Utilities", "UTIL", null, 1, Now);

        category.Update("Maintenance", "MAINT", "Repairs", 2, DateTimeOffset.UtcNow);

        category.Name.Should().Be("Maintenance");
        category.Code.Should().Be("MAINT");
        category.Description.Should().Be("Repairs");
        category.DisplayOrder.Should().Be(2);
        category.Version.Should().Be(2);
    }
}
