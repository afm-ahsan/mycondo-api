namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Seeds a tenant's default Expense Category catalogue (Template 3) and backfills any pre-existing
/// <c>ExpenseType</c> row's null <c>ExpenseCategoryId</c> (rows created before Template 3 introduced
/// categories) — same reconciled-by-<c>Code</c> approach as <c>IExpenseTypeCatalogueSeeder</c>. Must run
/// before <see cref="IExpenseTypeCatalogueSeeder"/> at tenant-bootstrap time so its default expense-type
/// rows have a category to attach to.
/// </summary>
public interface IExpenseCategoryCatalogueSeeder
{
    Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
