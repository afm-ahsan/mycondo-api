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

    /// <summary>Explicit Financial Statements (Phase 2A) reporting classification, when assigned —
    /// null for a custom tenant account that hasn't been classified. Use
    /// <see cref="EffectiveStatementGroup"/> for reporting; it never returns null.</summary>
    public FinancialStatementGroup? StatementGroup { get; private set; }

    /// <summary><see cref="StatementGroup"/> if explicitly assigned, otherwise the category's default
    /// "Other ..." bucket — guarantees this account is always included in a Financial Statement (Phase
    /// 2A plan §8/§15: unmapped accounts must never silently disappear).</summary>
    public FinancialStatementGroup EffectiveStatementGroup =>
        StatementGroup ?? FinancialStatementGroupCategories.DefaultFor(Category);

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
        LedgerDirection normalBalance, ChartOfAccountId? parentAccountId, bool isSystemAccount,
        FinancialStatementGroup? statementGroup) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Category = category;
        NormalBalance = normalBalance;
        ParentAccountId = parentAccountId;
        IsSystemAccount = isSystemAccount;
        IsActive = true;
        StatementGroup = statementGroup;
    }

    public static ChartOfAccount Create(
        Guid tenantId, string code, string name, AccountCategory category, LedgerDirection normalBalance,
        ChartOfAccountId? parentAccountId = null, bool isSystemAccount = false,
        FinancialStatementGroup? statementGroup = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (statementGroup is FinancialStatementGroup group && !FinancialStatementGroupCategories.IsValidFor(group, category))
        {
            throw new StatementGroupCategoryMismatchException(category, group);
        }

        return new ChartOfAccount(
            ChartOfAccountId.New(), tenantId, code.Trim(), name.Trim(), category, normalBalance,
            parentAccountId, isSystemAccount, statementGroup);
    }

    /// <summary>Assigns or clears this account's explicit Financial Statements classification. Allowed
    /// on system accounts too — reclassification is a reporting concern, not a posting-integrity one, so
    /// it is not gated by <see cref="SystemAccountCannotBeModifiedException"/>.</summary>
    public void Reclassify(FinancialStatementGroup? statementGroup)
    {
        if (statementGroup is FinancialStatementGroup group && !FinancialStatementGroupCategories.IsValidFor(group, Category))
        {
            throw new StatementGroupCategoryMismatchException(Category, group);
        }

        StatementGroup = statementGroup;
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
