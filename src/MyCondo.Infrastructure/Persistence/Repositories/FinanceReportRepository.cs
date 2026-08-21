using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FinanceReportRepository(MyCondoDbContext db) : IFinanceReportRepository
{
    public async Task<IReadOnlyList<TrialBalanceAccountLine>> GetTrialBalanceAsync(
        Guid tenantId, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        var grouped = await db.Set<LedgerEntry>()
            .Where(e => e.TenantId == tenantId && e.BusinessDate <= asOfDate && e.ChartOfAccountId != null)
            .GroupBy(e => e.ChartOfAccountId!.Value)
            .Select(g => new
            {
                ChartOfAccountId = g.Key,
                TotalDebit = g.Where(e => e.Direction == LedgerDirection.Debit).Sum(e => e.Amount),
                TotalCredit = g.Where(e => e.Direction == LedgerDirection.Credit).Sum(e => e.Amount),
            })
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
        {
            return [];
        }

        List<ChartOfAccountId> accountIds = grouped.Select(g => g.ChartOfAccountId).ToList();
        Dictionary<ChartOfAccountId, ChartOfAccount> accounts = await db.Set<ChartOfAccount>()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        return grouped
            .Where(g => accounts.ContainsKey(g.ChartOfAccountId))
            .Select(g =>
            {
                ChartOfAccount account = accounts[g.ChartOfAccountId];
                return new TrialBalanceAccountLine(
                    account.Id, account.Code, account.Name, account.Category, account.NormalBalance,
                    g.TotalDebit, g.TotalCredit);
            })
            .OrderBy(l => l.Code)
            .ToList();
    }

    public async Task<PagedResult<LedgerActivityLine>> GetGeneralLedgerAsync(
        Guid tenantId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? chartOfAccountId,
        Guid? fundId,
        string? referenceType,
        int page,
        int pageSize,
        bool ascending,
        CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntry> query = db.Set<LedgerEntry>().Where(e => e.TenantId == tenantId);

        if (fromDate is DateOnly from)
        {
            query = query.Where(e => e.BusinessDate >= from);
        }

        if (toDate is DateOnly to)
        {
            query = query.Where(e => e.BusinessDate <= to);
        }

        if (chartOfAccountId is Guid accountId)
        {
            ChartOfAccountId typedAccountId = new(accountId);
            query = query.Where(e => e.ChartOfAccountId == typedAccountId);
        }

        if (fundId is Guid fund)
        {
            FundId typedFundId = new(fund);
            query = query.Where(e => e.FundId == typedFundId);
        }

        var joined =
            from entry in query
            join posting in db.Set<LedgerPosting>() on entry.PostingId equals posting.Id
            where referenceType == null || posting.ReferenceType == referenceType
            select new { entry, posting };

        joined = ascending
            ? joined.OrderBy(x => x.entry.BusinessDate).ThenBy(x => x.posting.PostedAtUtc)
            : joined.OrderByDescending(x => x.entry.BusinessDate).ThenByDescending(x => x.posting.PostedAtUtc);

        long total = await joined.LongCountAsync(cancellationToken);

        var page1 = await joined
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.entry, x.posting })
            .ToListAsync(cancellationToken);

        List<ChartOfAccountId> accountIds = page1
            .Where(x => x.entry.ChartOfAccountId != null)
            .Select(x => x.entry.ChartOfAccountId!.Value)
            .Distinct()
            .ToList();

        Dictionary<ChartOfAccountId, ChartOfAccount> accounts = accountIds.Count == 0
            ? []
            : await db.Set<ChartOfAccount>().Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);

        List<LedgerActivityLine> items = page1
            .Select(x =>
            {
                ChartOfAccount? account = x.entry.ChartOfAccountId is ChartOfAccountId coaId && accounts.TryGetValue(coaId, out ChartOfAccount? found)
                    ? found
                    : null;

                return new LedgerActivityLine(
                    x.entry.Id, x.entry.PostingId, x.entry.BusinessDate, x.entry.Description,
                    x.posting.ReferenceType, x.posting.ReferenceId, x.entry.ChartOfAccountId,
                    account?.Code, account?.Name, x.entry.FlatId, x.entry.FundId, x.entry.Direction, x.entry.Amount);
            })
            .ToList();

        return new PagedResult<LedgerActivityLine>(items, page, pageSize, total);
    }

    public async Task<(decimal TotalDebit, decimal TotalCredit)> GetAccountBalanceBeforeAsync(
        Guid tenantId, Guid chartOfAccountId, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        ChartOfAccountId typedAccountId = new(chartOfAccountId);
        IQueryable<LedgerEntry> query = db.Set<LedgerEntry>()
            .Where(e => e.TenantId == tenantId && e.ChartOfAccountId == typedAccountId && e.BusinessDate < asOfDate);

        decimal totalDebit = await query.Where(e => e.Direction == LedgerDirection.Debit).SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        decimal totalCredit = await query.Where(e => e.Direction == LedgerDirection.Credit).SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        return (totalDebit, totalCredit);
    }

    public async Task<IReadOnlyList<FundActivityLine>> GetFundActivityAsync(
        Guid tenantId, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        List<Fund> funds = await db.Set<Fund>()
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.Code)
            .ToListAsync(cancellationToken);

        if (funds.Count == 0)
        {
            return [];
        }

        var grouped = await db.Set<LedgerEntry>()
            .Where(e => e.TenantId == tenantId && e.BusinessDate <= asOfDate && e.FundId != null)
            .GroupBy(e => e.FundId!.Value)
            .Select(g => new
            {
                FundId = g.Key,
                TotalDebit = g.Where(e => e.Direction == LedgerDirection.Debit).Sum(e => e.Amount),
                TotalCredit = g.Where(e => e.Direction == LedgerDirection.Credit).Sum(e => e.Amount),
            })
            .ToDictionaryAsync(g => g.FundId, cancellationToken);

        return funds
            .Select(f => grouped.TryGetValue(f.Id, out var activity)
                ? new FundActivityLine(f.Id, f.Code, f.Name, activity.TotalDebit, activity.TotalCredit)
                : new FundActivityLine(f.Id, f.Code, f.Name, 0m, 0m))
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetCashAndBankChartOfAccountIdsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        AccountMapping? cashOrBankMapping = await db.Set<AccountMapping>()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.PostingRole == nameof(LedgerAccountType.CashOrBank), cancellationToken);

        if (cashOrBankMapping is null)
        {
            return [];
        }

        List<Guid> childAccountIds = await db.Set<FinancialAccount>()
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.ChartOfAccountId.Value)
            .ToListAsync(cancellationToken);

        return childAccountIds
            .Append(cashOrBankMapping.ChartOfAccountId.Value)
            .Distinct()
            .ToList();
    }

    public async Task<IReadOnlyList<CashMovementLine>> GetCashMovementsAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> cashAccountIds = await GetCashAndBankChartOfAccountIdsAsync(tenantId, cancellationToken);
        if (cashAccountIds.Count == 0)
        {
            return [];
        }

        List<ChartOfAccountId> typedCashAccountIds = cashAccountIds.Select(id => new ChartOfAccountId(id)).ToList();

        List<LedgerEntry> cashEntries = await db.Set<LedgerEntry>()
            .Where(e => e.TenantId == tenantId
                && e.BusinessDate >= fromDate && e.BusinessDate <= toDate
                && e.ChartOfAccountId != null && typedCashAccountIds.Contains(e.ChartOfAccountId.Value))
            .ToListAsync(cancellationToken);

        if (cashEntries.Count == 0)
        {
            return [];
        }

        List<LedgerPostingId> postingIds = cashEntries.Select(e => e.PostingId).Distinct().ToList();

        List<LedgerPosting> postings = await db.Set<LedgerPosting>()
            .Where(p => postingIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        Dictionary<LedgerPostingId, LedgerPosting> postingsById = postings.ToDictionary(p => p.Id);

        List<LedgerEntry> counterEntries = await db.Set<LedgerEntry>()
            .Where(e => e.TenantId == tenantId && postingIds.Contains(e.PostingId)
                && (e.ChartOfAccountId == null || !typedCashAccountIds.Contains(e.ChartOfAccountId.Value)))
            .ToListAsync(cancellationToken);

        AccountMapping? fixedDepositMapping = await db.Set<AccountMapping>()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.PostingRole == nameof(LedgerAccountType.FixedDeposit), cancellationToken);
        ChartOfAccountId? fixedDepositAccountId = fixedDepositMapping?.ChartOfAccountId;

        ILookup<LedgerPostingId, LedgerEntry> counterEntriesByPosting = counterEntries.ToLookup(e => e.PostingId);

        List<CashMovementLine> result = [];
        foreach (LedgerEntry cashEntry in cashEntries)
        {
            LedgerPosting posting = postingsById[cashEntry.PostingId];

            LedgerEntry? largestCounterLeg = counterEntriesByPosting[cashEntry.PostingId]
                .OrderByDescending(e => e.Amount)
                .FirstOrDefault();

            bool isInvesting = fixedDepositAccountId is not null
                && largestCounterLeg?.ChartOfAccountId == fixedDepositAccountId;

            result.Add(new CashMovementLine(
                cashEntry.PostingId, cashEntry.BusinessDate, cashEntry.Direction, cashEntry.Amount,
                posting.ReferenceType, isInvesting));
        }

        return result;
    }

    public async Task<IReadOnlyList<TrialBalanceAccountLine>> GetCategoryActivityAsync(
        Guid tenantId, AccountCategory category, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var grouped = await (
                from e in db.Set<LedgerEntry>()
                join a in db.Set<ChartOfAccount>() on e.ChartOfAccountId equals a.Id
                where e.TenantId == tenantId && a.Category == category
                    && e.BusinessDate >= fromDate && e.BusinessDate <= toDate
                select new { e.ChartOfAccountId, e.Direction, e.Amount })
            .GroupBy(x => x.ChartOfAccountId)
            .Select(g => new
            {
                ChartOfAccountId = g.Key!.Value,
                TotalDebit = g.Where(x => x.Direction == LedgerDirection.Debit).Sum(x => x.Amount),
                TotalCredit = g.Where(x => x.Direction == LedgerDirection.Credit).Sum(x => x.Amount),
            })
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
        {
            return [];
        }

        List<ChartOfAccountId> accountIds = grouped.Select(g => g.ChartOfAccountId).ToList();
        Dictionary<ChartOfAccountId, ChartOfAccount> accounts = await db.Set<ChartOfAccount>()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        return grouped
            .Where(g => accounts.ContainsKey(g.ChartOfAccountId))
            .Select(g =>
            {
                ChartOfAccount account = accounts[g.ChartOfAccountId];
                return new TrialBalanceAccountLine(
                    account.Id, account.Code, account.Name, account.Category, account.NormalBalance,
                    g.TotalDebit, g.TotalCredit);
            })
            .OrderBy(l => l.Code)
            .ToList();
    }

    public async Task<IncomeCollectionSummary> GetIncomeCollectionAsync(
        Guid tenantId, LedgerAccountType incomeAccountType, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        IQueryable<Invoice> invoicesOfType = db.Set<Invoice>()
            .Where(i => i.TenantId == tenantId && i.IncomeAccountType == incomeAccountType);

        IQueryable<Invoice> billedInPeriod = invoicesOfType
            .Where(i => i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate && i.Status != InvoiceStatus.Void);

        decimal billed = await billedInPeriod.SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;
        int billedCount = await billedInPeriod.CountAsync(cancellationToken);
        decimal waived = await billedInPeriod.SumAsync(i => (decimal?)i.WaivedAmount, cancellationToken) ?? 0m;

        decimal collected = await (
                from alloc in db.Set<PaymentAllocation>()
                join inv in invoicesOfType on alloc.InvoiceId equals inv.Id
                join pay in db.Set<Payment>() on alloc.PaymentId equals pay.Id
                where alloc.TenantId == tenantId && pay.Status == PaymentStatus.Posted
                    && pay.BusinessDate >= fromDate && pay.BusinessDate <= toDate
                select alloc.AllocatedAmount)
            .SumAsync(cancellationToken: cancellationToken);

        return new IncomeCollectionSummary(billed, billedCount, collected, waived);
    }

    public async Task<Guid?> GetChartOfAccountIdForPostingRoleAsync(
        Guid tenantId, string postingRole, CancellationToken cancellationToken)
    {
        AccountMapping? mapping = await db.Set<AccountMapping>()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.PostingRole == postingRole, cancellationToken);

        return mapping?.ChartOfAccountId.Value;
    }

    public async Task<IReadOnlyList<MonthlyAccountActivityLine>> GetAccountMonthlyActivityAsync(
        Guid tenantId, Guid chartOfAccountId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        ChartOfAccountId typedAccountId = new(chartOfAccountId);

        return await db.Set<LedgerEntry>()
            .Where(e => e.TenantId == tenantId && e.ChartOfAccountId == typedAccountId
                && e.BusinessDate >= fromDate && e.BusinessDate <= toDate)
            .GroupBy(e => new { e.BusinessDate.Year, e.BusinessDate.Month })
            .Select(g => new MonthlyAccountActivityLine(
                g.Key.Year,
                g.Key.Month,
                g.Where(e => e.Direction == LedgerDirection.Debit).Sum(e => e.Amount),
                g.Where(e => e.Direction == LedgerDirection.Credit).Sum(e => e.Amount)))
            .OrderBy(l => l.Year).ThenBy(l => l.Month)
            .ToListAsync(cancellationToken);
    }
}
