using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Common.Services;

/// <summary>
/// Seeds Template 3's default Expense Category catalogue and backfills the category on any
/// pre-Template-3 <see cref="ExpenseType"/> row (<see cref="ExpenseType.ExpenseCategoryId"/> was
/// nullable specifically to allow this reconciliation instead of a destructive data migration — see
/// that property's doc comment). <see cref="LegacyExpenseTypeCodeToCategoryCode"/> maps the pre-Template-3
/// default catalogue's codes (<c>ExpenseTypeCatalogueSeeder</c>'s original set) onto a sensible category;
/// any other pre-existing type (tenant-custom or otherwise unmatched) backfills to <c>OTHER</c>.
/// </summary>
public sealed class ExpenseCategoryCatalogueSeeder(
    IExpenseCategoryRepository expenseCategories,
    IExpenseTypeRepository expenseTypes,
    ILogger<ExpenseCategoryCatalogueSeeder> logger
) : IExpenseCategoryCatalogueSeeder
{
    private static readonly (string Name, string Code, string Description)[] DefaultExpenseCategories =
    [
        ("Utilities", "UTILITIES", "Electricity, gas, water, and fuel for shared facilities."),
        ("Maintenance & Repairs", "MAINTENANCE", "Routine and corrective maintenance of common property."),
        ("Security", "SECURITY", "Security guard services and related costs."),
        ("Administrative & Professional", "ADMINISTRATIVE", "Office administration, legal, audit, and other professional fees."),
        ("Staffing & Payroll", "STAFFING", "Association staff wages and related costs."),
        ("Other / Miscellaneous", "OTHER", "Operational expenses not covered by another category."),
    ];

    private static readonly Dictionary<string, string> LegacyExpenseTypeCodeToCategoryCode = new(StringComparer.Ordinal)
    {
        ["CLEANING"] = "MAINTENANCE",
        ["SECURITY"] = "SECURITY",
        ["GENFUEL"] = "UTILITIES",
        ["LIFTMAINT"] = "MAINTENANCE",
        ["PLUMBING"] = "MAINTENANCE",
        ["ELECTRICAL"] = "MAINTENANCE",
        ["PESTCTRL"] = "MAINTENANCE",
        ["OFFICESUPPLY"] = "ADMINISTRATIVE",
        ["LEGALPROF"] = "ADMINISTRATIVE",
        ["REPAIRMAINT"] = "MAINTENANCE",
        ["MISC"] = "OTHER",
    };

    public async Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        List<ExpenseCategory> existing = await expenseCategories.GetAllForTenantAsync(tenantId, cancellationToken);
        HashSet<string> existingCodes = existing.Select(c => c.Code).ToHashSet(StringComparer.Ordinal);
        Dictionary<string, ExpenseCategory> byCode = existing.ToDictionary(c => c.Code, StringComparer.Ordinal);

        int created = 0;
        int displayOrder = 0;

        foreach ((string name, string code, string description) in DefaultExpenseCategories)
        {
            displayOrder++;

            if (existingCodes.Contains(code))
            {
                continue;
            }

            ExpenseCategory category = ExpenseCategory.Create(tenantId, name, code, description, displayOrder, nowUtc);
            expenseCategories.Add(category);
            byCode[code] = category;
            created++;
        }

        int backfilled = 0;
        if (byCode.TryGetValue("OTHER", out ExpenseCategory? fallbackCategory))
        {
            List<ExpenseType> types = await expenseTypes.GetAllForTenantAsync(tenantId, cancellationToken);
            foreach (ExpenseType type in types.Where(t => t.ExpenseCategoryId is null))
            {
                string categoryCode = LegacyExpenseTypeCodeToCategoryCode.GetValueOrDefault(type.Code, "OTHER");
                ExpenseCategory target = byCode.TryGetValue(categoryCode, out ExpenseCategory? mapped)
                    ? mapped
                    : fallbackCategory;

                type.BackfillExpenseCategory(target.Id);
                backfilled++;
            }
        }

        logger.LogInformation(
            "[DatabaseSeed] Expense category catalogue for tenant {TenantId}: {ExpectedCount} expected, " +
            "{Created} created, {Backfilled} expense type(s) backfilled with a category",
            tenantId, DefaultExpenseCategories.Length, created, backfilled);
    }
}
