using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Expenses.ExpenseCategories;

/// <summary>
/// The top of the Expense hierarchy — Expense Category → <see cref="ExpenseTypes.ExpenseType"/> →
/// <see cref="Expenses.Expense"/> (Template 3). Tenant-owned, editable data, same shape and
/// reconciliation approach as <see cref="ExpenseTypes.ExpenseType"/>: a practical default set is
/// application-seeded per tenant by <c>ExpenseCategoryCatalogueSeeder</c>, reconciled by
/// <see cref="Code"/>. Deliberately carries no budget/amount field — Template 3 explicitly keeps
/// budgeting out of the Category/Type catalogue; category/type-level financial analysis is achieved by
/// reporting through posted <c>LedgerEntry</c> rows via the source <see cref="Expenses.Expense"/> they
/// reference, not by duplicating amounts here. Once referenced by an <see cref="ExpenseTypes.ExpenseType"/>,
/// disable rather than delete — enforced by the application layer, since this aggregate has no way to
/// know whether it's referenced.
/// </summary>
public sealed class ExpenseCategory : AggregateRoot<ExpenseCategoryId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
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

    private ExpenseCategory()
    {
        Name = null!;
        Code = null!;
    }

    private ExpenseCategory(
        ExpenseCategoryId id,
        Guid tenantId,
        string name,
        string code,
        string? description,
        int displayOrder,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        Description = description;
        IsActive = true;
        DisplayOrder = displayOrder;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static ExpenseCategory Create(
        Guid tenantId, string name, string code, string? description, int displayOrder, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new ExpenseCategory(
            ExpenseCategoryId.New(), tenantId, name.Trim(), code.Trim().ToUpperInvariant(), description?.Trim(),
            displayOrder, nowUtc);
    }

    public void Update(string name, string code, string? description, int displayOrder, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = description?.Trim();
        DisplayOrder = displayOrder;
        Version++;
        UpdatedAtUtc = nowUtc;
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
