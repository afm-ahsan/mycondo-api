using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.AccountingPeriods.Exceptions;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Domain.Features.Finance.AccountingPeriods;

/// <summary>A sub-range of a <see cref="FinancialYear"/> (typically a calendar month) that new postings
/// get attributed to. Closing a period is what <c>IFinancialPostingService</c> checks before allowing a
/// posting whose business date falls inside it — see <see cref="AccountingPeriodClosedException"/>.</summary>
public sealed class AccountingPeriod : AggregateRoot<AccountingPeriodId>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public FinancialYearId FinancialYearId { get; private set; }
    public string Name { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public AccountingPeriodStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private AccountingPeriod()
    {
        Name = null!;
    }

    private AccountingPeriod(
        AccountingPeriodId id, Guid tenantId, FinancialYearId financialYearId, string name,
        DateOnly startDate, DateOnly endDate) : base(id)
    {
        TenantId = tenantId;
        FinancialYearId = financialYearId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = AccountingPeriodStatus.Open;
    }

    public static AccountingPeriod Create(
        Guid tenantId, FinancialYearId financialYearId, string name, DateOnly startDate, DateOnly endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (endDate <= startDate)
        {
            throw new ArgumentException("EndDate must be after StartDate.", nameof(endDate));
        }

        return new AccountingPeriod(AccountingPeriodId.New(), tenantId, financialYearId, name.Trim(), startDate, endDate);
    }

    public bool Covers(DateOnly businessDate) => businessDate >= StartDate && businessDate <= EndDate;

    /// <summary>Open → SoftClosed only. Softening an already-closed or already-soft-closed period is
    /// rejected rather than silently no-op'd, matching <see cref="Close"/>'s existing guard shape.</summary>
    public void SoftClose()
    {
        if (Status != AccountingPeriodStatus.Open)
        {
            throw new AccountingPeriodAlreadyClosedException(Id);
        }

        Status = AccountingPeriodStatus.SoftClosed;
    }

    /// <summary>Open or SoftClosed → Closed. Idempotent re-close is rejected, same as before Template 6.</summary>
    public void Close()
    {
        if (Status == AccountingPeriodStatus.Closed)
        {
            throw new AccountingPeriodAlreadyClosedException(Id);
        }

        Status = AccountingPeriodStatus.Closed;
    }

    /// <summary>SoftClosed or Closed → Open. Reopening an already-open period is now rejected — prior to
    /// Template 6 this was an unconditional no-op, which silently masked accidental reopen calls.</summary>
    public void Reopen()
    {
        if (Status == AccountingPeriodStatus.Open)
        {
            throw new AccountingPeriodAlreadyOpenException(Id);
        }

        Status = AccountingPeriodStatus.Open;
    }
}
