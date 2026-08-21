using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;

namespace MyCondo.Domain.Features.Expenses.ExpenseTypes;

/// <summary>
/// A tenant's expense type catalogue entry (e.g. Cleaning, Security, Generator Fuel), grouped under an
/// <see cref="ExpenseCategory"/> (Template 3: Expense Category → Expense Type → Expense). Tenant-
/// owned, editable data — every condominium can rename, add, or deactivate its own types — but a
/// practical default set is application-seeded per tenant at bootstrap time by
/// <c>ExpenseTypeCatalogueSeeder</c>, reconciled by <c>Code</c> the same way role catalogues are.
/// <see cref="ExpenseCategoryId"/> is nullable at the storage level only to accommodate rows created
/// before Template 3 introduced categories — <c>ExpenseCategoryCatalogueSeeder</c>'s backfill
/// step reconciles those; every type created or updated through <see cref="Create"/>/<see cref="Update"/>
/// going forward always has one. Deliberately carries no budget/amount field (Template 3 requirement) —
/// category/type-level financial analysis is achieved by reporting through posted <c>LedgerEntry</c> rows
/// via the source <see cref="Expenses.Expense"/> they reference, not by duplicating amounts here. Once
/// referenced by an <see cref="Expenses.Expense"/>, disable rather than delete — enforced by the
/// application layer, not here, since this aggregate has no way to know whether it's referenced.
/// </summary>
public sealed class ExpenseType : AggregateRoot<ExpenseTypeId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public ExpenseCategoryId? ExpenseCategoryId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private ExpenseType()
    {
        Name = null!;
        Code = null!;
    }

    private ExpenseType(
        ExpenseTypeId id,
        Guid tenantId,
        ExpenseCategoryId expenseCategoryId,
        string name,
        string code,
        string? description,
        int displayOrder,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        ExpenseCategoryId = expenseCategoryId;
        Name = name;
        Code = code;
        Description = description;
        IsActive = true;
        DisplayOrder = displayOrder;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static ExpenseType Create(
        Guid tenantId, ExpenseCategoryId expenseCategoryId, string name, string code, string? description,
        int displayOrder, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new ExpenseType(
            ExpenseTypeId.New(), tenantId, expenseCategoryId, name.Trim(), code.Trim().ToUpperInvariant(),
            description?.Trim(), displayOrder, nowUtc);
    }

    public void Update(
        ExpenseCategoryId expenseCategoryId, string name, string code, string? description, int displayOrder,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ExpenseCategoryId = expenseCategoryId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = description?.Trim();
        DisplayOrder = displayOrder;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Backfill-only setter used by <c>ExpenseCategoryCatalogueSeeder</c> to reconcile a
    /// pre-Template-3 row's category without bumping <see cref="Version"/>/<see cref="UpdatedAtUtc"/> —
    /// this is catalogue reconciliation, not a tenant-initiated edit.</summary>
    public void BackfillExpenseCategory(ExpenseCategoryId expenseCategoryId)
    {
        if (ExpenseCategoryId is null)
        {
            ExpenseCategoryId = expenseCategoryId;
        }
    }

    public void Deactivate(DateTimeOffset nowUtc)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTimeOffset nowUtc)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Version++;
        UpdatedAtUtc = nowUtc;
    }
}
