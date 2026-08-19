using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.ChartOfAccounts.Exceptions;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.Features.Finance.ChartOfAccounts;

/// <summary>
/// A tenant-configurable general-ledger account. The 6 accounts seeded 1:1 for the pre-existing
/// <see cref="LedgerAccountType"/> roles are created with <see cref="IsSystemAccount"/> = true and
/// cannot be deactivated or reparented — every existing ledger posting resolves to one of these via
/// <c>AccountMapping</c>, so removing one would silently break posting. Accounts added beyond those 6
/// (e.g. for future Expense/Bank-Reconciliation/Fixed-Deposit postings) are ordinary tenant-managed
/// accounts. See ADR-027.
/// </summary>
public sealed class ChartOfAccount : AggregateRoot<ChartOfAccountId>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public AccountCategory Category { get; private set; }
    public LedgerDirection NormalBalance { get; private set; }
    public ChartOfAccountId? ParentAccountId { get; private set; }
    public bool IsSystemAccount { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private ChartOfAccount()
    {
        Code = null!;
        Name = null!;
    }

    private ChartOfAccount(
        ChartOfAccountId id, Guid tenantId, string code, string name, AccountCategory category,
        LedgerDirection normalBalance, ChartOfAccountId? parentAccountId, bool isSystemAccount) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Category = category;
        NormalBalance = normalBalance;
        ParentAccountId = parentAccountId;
        IsSystemAccount = isSystemAccount;
        IsActive = true;
    }

    public static ChartOfAccount Create(
        Guid tenantId, string code, string name, AccountCategory category, LedgerDirection normalBalance,
        ChartOfAccountId? parentAccountId = null, bool isSystemAccount = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new ChartOfAccount(
            ChartOfAccountId.New(), tenantId, code.Trim(), name.Trim(), category, normalBalance,
            parentAccountId, isSystemAccount);
    }

    public void Deactivate()
    {
        if (IsSystemAccount)
        {
            throw new SystemAccountCannotBeModifiedException(Id);
        }

        IsActive = false;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
