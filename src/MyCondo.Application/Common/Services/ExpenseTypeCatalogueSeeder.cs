using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Common.Services;

public sealed class ExpenseTypeCatalogueSeeder(
    IExpenseTypeRepository expenseTypes,
    ILogger<ExpenseTypeCatalogueSeeder> logger
) : IExpenseTypeCatalogueSeeder
{
    /// <summary>
    /// A practical default operational expense category set for a newly bootstrapped condominium.
    /// <c>Code</c> is the stable natural key reconciliation matches on — never reuse a code for a
    /// different category, and never repurpose a retired one.
    /// </summary>
    private static readonly (string Name, string Code, string Description)[] DefaultExpenseTypes =
    [
        ("Cleaning", "CLEANING", "Routine cleaning and janitorial services."),
        ("Security", "SECURITY", "Security guard services and related costs."),
        ("Generator Fuel", "GENFUEL", "Fuel for backup generators."),
        ("Lift Maintenance", "LIFTMAINT", "Elevator/lift servicing and repairs."),
        ("Plumbing", "PLUMBING", "Plumbing repairs and maintenance."),
        ("Electrical", "ELECTRICAL", "Electrical repairs and maintenance."),
        ("Pest Control", "PESTCTRL", "Pest control and fumigation services."),
        ("Office Supplies", "OFFICESUPPLY", "Administrative and office supplies."),
        ("Legal & Professional", "LEGALPROF", "Legal, audit, and other professional fees."),
        ("Repair & Maintenance", "REPAIRMAINT", "General building repair and maintenance work not covered by a more specific category."),
        ("Miscellaneous", "MISC", "Operational expenses not covered by another category."),
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

        int created = 0;
        int displayOrder = 0;

        foreach ((string name, string code, string description) in DefaultExpenseTypes)
        {
            displayOrder++;

            if (existingCodes.Contains(code))
            {
                continue;
            }

            expenseTypes.Add(ExpenseType.Create(tenantId, name, code, description, displayOrder, nowUtc));
            created++;
        }

        logger.LogInformation(
            "[DatabaseSeed] Expense type catalogue for tenant {TenantId}: {ExpectedCount} expected, " +
            "{Created} created",
            tenantId, DefaultExpenseTypes.Length, created);
    }
}
