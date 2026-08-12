using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

public class ExpenseTypeCatalogueSeederTests
{
    private static readonly string[] ExpectedCodes =
    [
        "CLEANING", "SECURITY", "GENFUEL", "LIFTMAINT", "PLUMBING", "ELECTRICAL",
        "PESTCTRL", "OFFICESUPPLY", "LEGALPROF", "REPAIRMAINT", "MISC",
    ];

    private static (IExpenseTypeRepository ExpenseTypes, ILogger<ExpenseTypeCatalogueSeeder> Logger)
        BuildSubstitutes(IEnumerable<ExpenseType>? existing = null)
    {
        IExpenseTypeRepository expenseTypes = Substitute.For<IExpenseTypeRepository>();
        ILogger<ExpenseTypeCatalogueSeeder> logger = Substitute.For<ILogger<ExpenseTypeCatalogueSeeder>>();

        expenseTypes.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((existing ?? []).ToList());

        return (expenseTypes, logger);
    }

    [Fact]
    public async Task SeedAsync_Creates_The_Eleven_Default_Expense_Types_For_A_New_Tenant()
    {
        (IExpenseTypeRepository expenseTypes, ILogger<ExpenseTypeCatalogueSeeder> logger) = BuildSubstitutes();

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, logger);
        Guid tenantId = Guid.NewGuid();
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().HaveCount(11);
        added.Select(e => e.Code).Should().BeEquivalentTo(ExpectedCodes);
        added.Should().OnlyContain(e => e.TenantId == tenantId && e.IsActive);
        added.Select(e => e.DisplayOrder).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SeedAsync_Does_Not_Duplicate_A_Default_Type_That_Already_Exists_By_Code()
    {
        Guid tenantId = Guid.NewGuid();
        ExpenseType existingCleaning = ExpenseType.Create(
            tenantId, "Cleaning (renamed)", "CLEANING", "custom description", 5, DateTimeOffset.UtcNow);

        (IExpenseTypeRepository expenseTypes, ILogger<ExpenseTypeCatalogueSeeder> logger) =
            BuildSubstitutes([existingCleaning]);

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().HaveCount(10);
        added.Should().NotContain(e => e.Code == "CLEANING");
    }

    [Fact]
    public async Task SeedAsync_Does_Not_Recreate_A_Default_Type_The_Tenant_Deactivated()
    {
        Guid tenantId = Guid.NewGuid();
        ExpenseType deactivated = ExpenseType.Create(
            tenantId, "Security", "SECURITY", null, 2, DateTimeOffset.UtcNow);
        deactivated.Deactivate(DateTimeOffset.UtcNow);

        (IExpenseTypeRepository expenseTypes, ILogger<ExpenseTypeCatalogueSeeder> logger) =
            BuildSubstitutes([deactivated]);

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().NotContain(e => e.Code == "SECURITY");
    }

    [Fact]
    public async Task SeedAsync_Adds_Only_The_Missing_Types_When_Some_Already_Exist()
    {
        Guid tenantId = Guid.NewGuid();
        List<ExpenseType> existing =
        [
            ExpenseType.Create(tenantId, "Cleaning", "CLEANING", null, 1, DateTimeOffset.UtcNow),
            ExpenseType.Create(tenantId, "Security", "SECURITY", null, 2, DateTimeOffset.UtcNow),
        ];

        (IExpenseTypeRepository expenseTypes, ILogger<ExpenseTypeCatalogueSeeder> logger) =
            BuildSubstitutes(existing);

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().HaveCount(9);
        added.Select(e => e.Code).Should().NotContain(["CLEANING", "SECURITY"]);
    }
}
