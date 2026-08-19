using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Common.Services;

public sealed class ExpenseTypeCatalogueSeeder(
    IExpenseTypeRepository expenseTypes,
    IExpenseCategoryRepository expenseCategories,
    ILogger<ExpenseTypeCatalogueSeeder> logger
) : IExpenseTypeCatalogueSeeder
{
    /// <summary>
    /// A practical default operational expense category set for a newly bootstrapped condominium.
    /// <c>Code</c> is the stable natural key reconciliation matches on — never reuse a code for a
    /// different category, and never repurpose a retired one. <c>CategoryCode</c> must match one of
    /// <c>ExpenseCategoryCatalogueSeeder</c>'s default category codes — that seeder runs first at
    /// tenant-bootstrap time (see <c>IExpenseCategoryCatalogueSeeder</c>'s doc comment) so the category
    /// always exists by the time this seeder needs it.
    /// </summary>
    private static readonly (string Name, string Code, string CategoryCode, string Description)[] DefaultExpenseTypes =
    [
        ("Cleaning", "CLEANING", "MAINTENANCE", "Routine cleaning and janitorial services."),
        ("Security", "SECURITY", "SECURITY", "Security guard services and related costs."),
        ("Generator Fuel", "GENFUEL", "UTILITIES", "Fuel for backup generators."),
        ("Lift Maintenance", "LIFTMAINT", "MAINTENANCE", "Elevator/lift servicing and repairs."),
        ("Plumbing", "PLUMBING", "MAINTENANCE", "Plumbing repairs and maintenance."),
        ("Electrical", "ELECTRICAL", "MAINTENANCE", "Electrical repairs and maintenance."),
        ("Pest Control", "PESTCTRL", "MAINTENANCE", "Pest control and fumigation services."),
        ("Office Supplies", "OFFICESUPPLY", "ADMINISTRATIVE", "Administrative and office supplies."),
        ("Legal & Professional", "LEGALPROF", "ADMINISTRATIVE", "Legal, audit, and other professional fees."),
        ("Repair & Maintenance", "REPAIRMAINT", "MAINTENANCE", "General building repair and maintenance work not covered by a more specific category."),
        ("Miscellaneous", "MISC", "OTHER", "Operational expenses not covered by another category."),
    ];

    /// <summary>
    /// Reconciles by <c>Code</c> rather than unconditionally creating — safe to call on every
    /// tenant-bootstrap run (not just the first), so a category added to the default catalogue after a
    /// tenant already exists still reaches it. Never updates or removes an existing expense type, even
    /// one the tenant has since renamed or deactivated.
    /// </summary>
    public async Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        List<ExpenseType> existing = await expenseTypes.GetAllForTenantAsync(tenantId, cancellationToken);
        HashSet<string> existingCodes = existing.Select(e => e.Code).ToHashSet(StringComparer.Ordinal);

        List<ExpenseCategory> categories = await expenseCategories.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<string, ExpenseCategory> categoriesByCode = categories.ToDictionary(c => c.Code, StringComparer.Ordinal);

        int created = 0;
        int skippedNoCategory = 0;
        int displayOrder = 0;

        foreach ((string name, string code, string categoryCode, string description) in DefaultExpenseTypes)
        {
            displayOrder++;

            if (existingCodes.Contains(code))
            {
                continue;
            }

            if (!categoriesByCode.TryGetValue(categoryCode, out ExpenseCategory? category))
            {
                // The category catalogue seeder hasn't run yet for this tenant — skip rather than fail;
                // the next reconciliation pass (this seeder is safe to re-run) picks it up once it has.
                skippedNoCategory++;
                continue;
            }

            expenseTypes.Add(ExpenseType.Create(tenantId, category.Id, name, code, description, displayOrder, nowUtc));
            created++;
        }

        logger.LogInformation(
            "[DatabaseSeed] Expense type catalogue for tenant {TenantId}: {ExpectedCount} expected, " +
            "{Created} created, {SkippedNoCategory} skipped (category not yet seeded)",
            tenantId, DefaultExpenseTypes.Length, created, skippedNoCategory);
    }
}
