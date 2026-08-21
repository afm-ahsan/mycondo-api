using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
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

    private static List<ExpenseCategory> AllCategories(Guid tenantId) =>
    [
        ExpenseCategory.Create(tenantId, "Utilities", "UTILITIES", null, 1, DateTimeOffset.UtcNow),
        ExpenseCategory.Create(tenantId, "Maintenance & Repairs", "MAINTENANCE", null, 2, DateTimeOffset.UtcNow),
        ExpenseCategory.Create(tenantId, "Security", "SECURITY", null, 3, DateTimeOffset.UtcNow),
        ExpenseCategory.Create(tenantId, "Administrative & Professional", "ADMINISTRATIVE", null, 4, DateTimeOffset.UtcNow),
        ExpenseCategory.Create(tenantId, "Staffing & Payroll", "STAFFING", null, 5, DateTimeOffset.UtcNow),
        ExpenseCategory.Create(tenantId, "Other / Miscellaneous", "OTHER", null, 6, DateTimeOffset.UtcNow),
    ];

    private static (IExpenseTypeRepository ExpenseTypes, IExpenseCategoryRepository ExpenseCategories, ILogger<ExpenseTypeCatalogueSeeder> Logger)
        BuildSubstitutes(Guid tenantId, IEnumerable<ExpenseType>? existing = null, IEnumerable<ExpenseCategory>? categories = null)
    {
        IExpenseTypeRepository expenseTypes = Substitute.For<IExpenseTypeRepository>();
        IExpenseCategoryRepository expenseCategories = Substitute.For<IExpenseCategoryRepository>();
        ILogger<ExpenseTypeCatalogueSeeder> logger = Substitute.For<ILogger<ExpenseTypeCatalogueSeeder>>();

        expenseTypes.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((existing ?? []).ToList());
        expenseCategories.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((categories ?? AllCategories(tenantId)).ToList());

        return (expenseTypes, expenseCategories, logger);
    }

    [Fact]
    public async Task SeedAsync_Creates_The_Eleven_Default_Expense_Types_For_A_New_Tenant()
    {
        Guid tenantId = Guid.NewGuid();
        (IExpenseTypeRepository expenseTypes, IExpenseCategoryRepository expenseCategories, ILogger<ExpenseTypeCatalogueSeeder> logger) =
            BuildSubstitutes(tenantId);

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, expenseCategories, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().HaveCount(11);
        added.Select(e => e.Code).Should().BeEquivalentTo(ExpectedCodes);
        added.Should().OnlyContain(e => e.TenantId == tenantId && e.IsActive && e.ExpenseCategoryId != null);
        added.Select(e => e.DisplayOrder).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SeedAsync_Does_Not_Duplicate_A_Default_Type_That_Already_Exists_By_Code()
    {
        Guid tenantId = Guid.NewGuid();
        ExpenseCategoryId categoryId = ExpenseCategoryId.New();
        ExpenseType existingCleaning = ExpenseType.Create(
            tenantId, categoryId, "Cleaning (renamed)", "CLEANING", "custom description", 5, DateTimeOffset.UtcNow);

        (IExpenseTypeRepository expenseTypes, IExpenseCategoryRepository expenseCategories, ILogger<ExpenseTypeCatalogueSeeder> logger) =
            BuildSubstitutes(tenantId, [existingCleaning]);

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, expenseCategories, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().HaveCount(10);
        added.Should().NotContain(e => e.Code == "CLEANING");
    }

    [Fact]
    public async Task SeedAsync_Does_Not_Recreate_A_Default_Type_The_Tenant_Deactivated()
    {
        Guid tenantId = Guid.NewGuid();
        ExpenseType deactivated = ExpenseType.Create(
            tenantId, ExpenseCategoryId.New(), "Security", "SECURITY", null, 2, DateTimeOffset.UtcNow);
        deactivated.Deactivate(DateTimeOffset.UtcNow);

        (IExpenseTypeRepository expenseTypes, IExpenseCategoryRepository expenseCategories, ILogger<ExpenseTypeCatalogueSeeder> logger) =
            BuildSubstitutes(tenantId, [deactivated]);

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, expenseCategories, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().NotContain(e => e.Code == "SECURITY");
    }

    [Fact]
    public async Task SeedAsync_Adds_Only_The_Missing_Types_When_Some_Already_Exist()
    {
        Guid tenantId = Guid.NewGuid();
        ExpenseCategoryId categoryId = ExpenseCategoryId.New();
        List<ExpenseType> existing =
        [
            ExpenseType.Create(tenantId, categoryId, "Cleaning", "CLEANING", null, 1, DateTimeOffset.UtcNow),
            ExpenseType.Create(tenantId, categoryId, "Security", "SECURITY", null, 2, DateTimeOffset.UtcNow),
        ];

        (IExpenseTypeRepository expenseTypes, IExpenseCategoryRepository expenseCategories, ILogger<ExpenseTypeCatalogueSeeder> logger) =
            BuildSubstitutes(tenantId, existing);

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, expenseCategories, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().HaveCount(9);
        added.Select(e => e.Code).Should().NotContain(["CLEANING", "SECURITY"]);
    }

    [Fact]
    public async Task SeedAsync_Skips_Default_Types_Whose_Category_Is_Not_Yet_Seeded()
    {
        Guid tenantId = Guid.NewGuid();
        (IExpenseTypeRepository expenseTypes, IExpenseCategoryRepository expenseCategories, ILogger<ExpenseTypeCatalogueSeeder> logger) =
            BuildSubstitutes(tenantId, categories: []);

        List<ExpenseType> added = [];
        expenseTypes.Add(Arg.Do<ExpenseType>(added.Add));

        ExpenseTypeCatalogueSeeder seeder = new(expenseTypes, expenseCategories, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        added.Should().BeEmpty();
    }
}
