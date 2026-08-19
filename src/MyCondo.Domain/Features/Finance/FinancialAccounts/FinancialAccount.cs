using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Domain.Features.Finance.FinancialAccounts;

/// <summary>
/// A tenant's physical money location (Template 4) — Cash on hand, a bank account, or a Mobile
/// Financial Service wallet — kept distinct from <see cref="Fund"/> (a Fund is "whose money"; a
/// Financial Account is "where the money physically sits" — independent dimensions, per the Template 4
/// spec's "Fixed Deposit != Spendable Cash" framing). Each account owns its own
/// <see cref="ChartOfAccountId"/> — created as a child of the tenant's single system <c>CashOrBank</c>
/// account — so postings against a specific account (see <c>IFinancialPostingService</c>'s
/// <c>FinancialPostingLine.ExplicitAccountId</c>) are individually traceable/reconcilable, while every
/// pre-Template-4 call site that only names the <c>CashOrBank</c> role keeps resolving to the tenant's
/// original default account, unaffected (ADR-027).
/// </summary>
public sealed class FinancialAccount : AggregateRoot<FinancialAccountId>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public FinancialAccountType AccountType { get; private set; }
    public string? BankName { get; private set; }
    public string? BranchName { get; private set; }
    public string? AccountNumber { get; private set; }

    /// <summary>This account's own asset ledger account — see this type's doc comment.</summary>
    public ChartOfAccountId ChartOfAccountId { get; private set; }

    public FundId? FundId { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private FinancialAccount()
    {
        Name = null!;
    }

    private FinancialAccount(
        FinancialAccountId id, Guid tenantId, string name, FinancialAccountType accountType, string? bankName,
        string? branchName, string? accountNumber, ChartOfAccountId chartOfAccountId, FundId? fundId,
        string? notes) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        AccountType = accountType;
        BankName = bankName;
        BranchName = branchName;
        AccountNumber = accountNumber;
        ChartOfAccountId = chartOfAccountId;
        FundId = fundId;
        Notes = notes;
        IsActive = true;
    }

    public static FinancialAccount Create(
        Guid tenantId, string name, FinancialAccountType accountType, string? bankName, string? branchName,
        string? accountNumber, ChartOfAccountId chartOfAccountId, FundId? fundId, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new FinancialAccount(
            FinancialAccountId.New(), tenantId, name.Trim(), accountType, bankName?.Trim(), branchName?.Trim(),
            accountNumber?.Trim(), chartOfAccountId, fundId, notes?.Trim());
    }

    /// <summary>Renaming/re-detailing the account never touches its <see cref="ChartOfAccountId"/> or
    /// historical postings — see the execution prompt's "preserve historical bank/account attribution
    /// even if an account is later renamed" requirement.</summary>
    public void Update(string name, string? bankName, string? branchName, string? accountNumber, FundId? fundId, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        BankName = bankName?.Trim();
        BranchName = branchName?.Trim();
        AccountNumber = accountNumber?.Trim();
        FundId = fundId;
        Notes = notes?.Trim();
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
