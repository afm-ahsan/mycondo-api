using AwesomeAssertions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Domain.UnitTests.Features.Expenses.ExpenseTypes;

public class ExpenseTypeTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Trims_Name_And_Uppercases_Code()
    {
        ExpenseType expenseType = ExpenseType.Create(TenantId, "  Cleaning  ", " clean ", " routine ", 1, Now);

        expenseType.Name.Should().Be("Cleaning");
        expenseType.Code.Should().Be("CLEAN");
        expenseType.Description.Should().Be("routine");
        expenseType.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_Then_Activate_Round_Trips_And_Bumps_Version_Each_Time()
    {
        ExpenseType expenseType = ExpenseType.Create(TenantId, "Cleaning", "CLN", null, 1, Now);

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
        ExpenseType expenseType = ExpenseType.Create(TenantId, "Cleaning", "CLN", null, 1, Now);
        expenseType.Deactivate(DateTimeOffset.UtcNow);
        int versionAfterFirstDeactivate = expenseType.Version;

        expenseType.Deactivate(DateTimeOffset.UtcNow);

        expenseType.Version.Should().Be(versionAfterFirstDeactivate);
    }
}
