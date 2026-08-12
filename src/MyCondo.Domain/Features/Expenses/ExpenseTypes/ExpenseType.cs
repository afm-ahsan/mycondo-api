using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Expenses.ExpenseTypes;

/// <summary>
/// A tenant's expense category catalogue entry (e.g. Cleaning, Security, Generator Fuel). Tenant-
/// owned, editable data — every condominium can rename, add, or deactivate its own categories — but a
/// practical default set is application-seeded per tenant at bootstrap time by
/// <c>ExpenseTypeCatalogueSeeder</c>, reconciled by <c>Code</c> the same way role catalogues are.
/// Once referenced by an <see cref="Expenses.Expense"/>, disable rather than delete — enforced by the
/// application layer, not here, since this aggregate has no way to know whether it's referenced.
/// </summary>
public sealed class ExpenseType : AggregateRoot<ExpenseTypeId>, IAuditable, ITenantScoped
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

    private ExpenseType()
    {
        Name = null!;
        Code = null!;
    }

    private ExpenseType(
        ExpenseTypeId id,
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

    public static ExpenseType Create(
        Guid tenantId, string name, string code, string? description, int displayOrder, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new ExpenseType(
            ExpenseTypeId.New(), tenantId, name.Trim(), code.Trim().ToUpperInvariant(), description?.Trim(),
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
