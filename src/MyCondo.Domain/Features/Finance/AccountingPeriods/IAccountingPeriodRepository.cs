using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Domain.Features.Finance.AccountingPeriods;

public interface IAccountingPeriodRepository
{
    void Add(AccountingPeriod period);

    Task<AccountingPeriod?> GetByIdAsync(AccountingPeriodId id, CancellationToken cancellationToken);

    /// <summary>The open-or-closed period covering <paramref name="businessDate"/>, if one has been
    /// defined — used by <c>IFinancialPostingService</c> both to stamp the new dimension column and to
    /// enforce closed-period prevention. Returns null when no period has been configured for that date
    /// (postings are still allowed; the dimension is simply left unset, matching how a date before any
    /// financial-year setup has no period to attribute to).</summary>
    Task<AccountingPeriod?> FindCoveringAsync(Guid tenantId, DateOnly businessDate, CancellationToken cancellationToken);

    Task<bool> OverlapsAsync(Guid tenantId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountingPeriod>> GetAllForFinancialYearAsync(FinancialYearId financialYearId, CancellationToken cancellationToken);
}
