using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IFinancialStatementNoteRepository"/>
public sealed class FinancialStatementNoteRepository(MyCondoDbContext db) : IFinancialStatementNoteRepository
{
    public async Task<IReadOnlyList<NoteAccountBalanceLine>> GetNoteAccountBalancesAsync(
        Guid tenantId, IReadOnlyList<Guid> chartOfAccountIds, DateOnly? fromDate, DateOnly toDate,
        Guid? fundId, CancellationToken cancellationToken)
    {
        NoteEntryFilter? filter = BuildEntryFilter(tenantId, chartOfAccountIds, fromDate, toDate, fundId);
        if (filter is null)
        {
            return [];
        }

        var scoped =
            from entry in filter.Entries
            join account in db.Set<ChartOfAccount>() on entry.ChartOfAccountId equals account.Id
            where filter.AccountIds.Contains(account.Id)
            select new { AccountId = account.Id, entry.Direction, entry.Amount };

        return await scoped
            .GroupBy(x => x.AccountId)
            .Select(g => new NoteAccountBalanceLine(
                g.Key,
                g.Where(x => x.Direction == LedgerDirection.Debit).Sum(x => x.Amount),
                g.Where(x => x.Direction == LedgerDirection.Credit).Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoteFlatBalanceLine>> GetNoteFlatBalancesAsync(
        Guid tenantId, IReadOnlyList<Guid> chartOfAccountIds, DateOnly? fromDate, DateOnly toDate,
        Guid? fundId, CancellationToken cancellationToken)
    {
        NoteEntryFilter? filter = BuildEntryFilter(tenantId, chartOfAccountIds, fromDate, toDate, fundId);
        if (filter is null)
        {
            return [];
        }

        var scoped =
            from entry in filter.Entries
            join account in db.Set<ChartOfAccount>() on entry.ChartOfAccountId equals account.Id
            where filter.AccountIds.Contains(account.Id)
            select new { entry.FlatId, entry.Direction, entry.Amount };

        return await scoped
            .GroupBy(x => x.FlatId)
            .Select(g => new NoteFlatBalanceLine(
                g.Key,
                g.Where(x => x.Direction == LedgerDirection.Debit).Sum(x => x.Amount),
                g.Where(x => x.Direction == LedgerDirection.Credit).Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotePostingReferenceLine>> GetNotePostingReferenceBalancesAsync(
        Guid tenantId, IReadOnlyList<Guid> chartOfAccountIds, DateOnly? fromDate, DateOnly toDate,
        Guid? fundId, CancellationToken cancellationToken)
    {
        NoteEntryFilter? filter = BuildEntryFilter(tenantId, chartOfAccountIds, fromDate, toDate, fundId);
        if (filter is null)
        {
            return [];
        }

        var joined =
            from entry in filter.Entries
            join account in db.Set<ChartOfAccount>() on entry.ChartOfAccountId equals account.Id
            join posting in db.Set<LedgerPosting>() on entry.PostingId equals posting.Id
            where filter.AccountIds.Contains(account.Id)
            select new { posting.ReferenceType, posting.ReferenceId, entry.Direction, entry.Amount };

        return await joined
            .GroupBy(x => new { x.ReferenceType, x.ReferenceId })
            .Select(g => new NotePostingReferenceLine(
                g.Key.ReferenceType,
                g.Key.ReferenceId,
                g.Where(x => x.Direction == LedgerDirection.Debit).Sum(x => x.Amount),
                g.Where(x => x.Direction == LedgerDirection.Credit).Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotePostingReference>> GetPostingReferencesAsync(
        Guid tenantId, IReadOnlyList<Guid> postingIds, CancellationToken cancellationToken)
    {
        if (postingIds.Count == 0)
        {
            return [];
        }

        List<LedgerPostingId> typedIds = postingIds.Distinct().Select(id => new LedgerPostingId(id)).ToList();

        return await db.Set<LedgerPosting>()
            .Where(p => p.TenantId == tenantId && typedIds.Contains(p.Id))
            .Select(p => new NotePostingReference(p.Id, p.ReferenceType, p.ReferenceId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoteFinancialAccountRef>> GetFinancialAccountRefsAsync(
        Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<FinancialAccount>()
            .Where(a => a.TenantId == tenantId)
            .Select(a => new NoteFinancialAccountRef(
                a.Id, a.ChartOfAccountId, a.Name, a.AccountType, a.BankName, a.BranchName,
                a.AccountNumber, a.FundId, a.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NoteFixedDepositRef>> GetFixedDepositRefsAsync(
        Guid tenantId, IReadOnlyList<Guid> fixedDepositIds, CancellationToken cancellationToken)
    {
        if (fixedDepositIds.Count == 0)
        {
            return [];
        }

        List<FixedDepositId> typedIds = fixedDepositIds.Distinct().Select(id => new FixedDepositId(id)).ToList();

        return await db.Set<FixedDeposit>()
            .Where(f => f.TenantId == tenantId && typedIds.Contains(f.Id))
            .Select(f => new NoteFixedDepositRef(
                f.Id, f.CertificateNumber, f.BankName, f.BranchName, f.StartDate, f.MaturityDate,
                f.Principal, f.InterestRatePercent, f.Status, f.FundId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoteFixedDepositInterestSourceRef>> GetFixedDepositInterestSourceRefsAsync(
        Guid tenantId, IReadOnlyList<Guid> sourceIds, CancellationToken cancellationToken)
    {
        if (sourceIds.Count == 0)
        {
            return [];
        }

        List<Guid> distinctIds = sourceIds.Distinct().ToList();

        List<FixedDepositInterestAccrualId> accrualIds =
            distinctIds.Select(id => new FixedDepositInterestAccrualId(id)).ToList();
        List<FixedDepositInterestReceiptId> receiptIds =
            distinctIds.Select(id => new FixedDepositInterestReceiptId(id)).ToList();

        List<NoteFixedDepositInterestSourceRef> accruals = await db.Set<FixedDepositInterestAccrual>()
            .Where(a => a.TenantId == tenantId && accrualIds.Contains(a.Id))
            .Select(a => new NoteFixedDepositInterestSourceRef(
                a.Id.Value, a.FixedDepositId, a.GrossAmount, 0m, false))
            .ToListAsync(cancellationToken);

        List<NoteFixedDepositInterestSourceRef> receipts = await db.Set<FixedDepositInterestReceipt>()
            .Where(r => r.TenantId == tenantId && receiptIds.Contains(r.Id))
            .Select(r => new NoteFixedDepositInterestSourceRef(
                r.Id.Value, r.FixedDepositId, r.GrossAmount, r.DeductionAmount, true))
            .ToListAsync(cancellationToken);

        return [.. accruals, .. receipts];
    }

    public async Task<IReadOnlyList<NoteExpenseRef>> GetExpenseRefsAsync(
        Guid tenantId, IReadOnlyList<Guid> expenseIds, CancellationToken cancellationToken)
    {
        if (expenseIds.Count == 0)
        {
            return [];
        }

        List<ExpenseId> typedIds = expenseIds.Distinct().Select(id => new ExpenseId(id)).ToList();

        return await (
                from expense in db.Set<Expense>()
                join type in db.Set<ExpenseType>() on expense.ExpenseTypeId equals type.Id
                join category in db.Set<ExpenseCategory>() on type.ExpenseCategoryId equals category.Id into categories
                from category in categories.DefaultIfEmpty()
                where expense.TenantId == tenantId && typedIds.Contains(expense.Id)
                select new NoteExpenseRef(
                    expense.Id, expense.Description, expense.Payee, expense.ReferenceNumber,
                    expense.AccountingDate, expense.Amount, expense.Status,
                    category != null ? category.Id : null,
                    category != null ? category.Name : null,
                    type.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoteFlatRef>> GetFlatRefsAsync(
        Guid tenantId, IReadOnlyList<Guid> flatIds, CancellationToken cancellationToken)
    {
        if (flatIds.Count == 0)
        {
            return [];
        }

        List<FlatId> typedIds = flatIds.Distinct().Select(id => new FlatId(id)).ToList();

        return await (
                from flat in db.Set<Flat>()
                join building in db.Set<Building>() on flat.BuildingId equals building.Id
                where flat.TenantId == tenantId && typedIds.Contains(flat.Id)
                select new NoteFlatRef(flat.Id, flat.FlatNumber, building.Id.Value, building.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChartOfAccount>> GetChartOfAccountsAsync(
        Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<ChartOfAccount>()
            .Where(a => a.TenantId == tenantId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <summary>The tenant/date/fund-filtered ledger rows plus the account set every note decomposition
    /// restricts to. Callers join to <c>ChartOfAccount</c> themselves and filter on
    /// <see cref="AccountIds"/> against <c>ChartOfAccount.Id</c> (non-nullable) rather than
    /// <c>LedgerEntry.ChartOfAccountId</c> (nullable) — Npgsql cannot build an array parameter whose
    /// element type is a nullable strongly-typed id, so <c>Contains</c> over the nullable column throws
    /// at translation time. The join also subsumes the "entry has a resolved account" filter, matching
    /// the pattern <c>FinanceReportRepository.GetCategoryActivityAsync</c> already uses. Both this and
    /// the "project to an anonymous type before grouping, never to a named record" rule below were
    /// established by real-PostgreSQL failures in <c>FinancialStatementNoteQueryRlsTests</c>: EF cannot
    /// see through a record constructor in a <c>GroupBy</c> key.</summary>
    private sealed record NoteEntryFilter(IQueryable<LedgerEntry> Entries, List<ChartOfAccountId> AccountIds);

    /// <summary>Builds the one filter every note decomposition shares — tenant, date window and optional
    /// fund, applied identically so a schedule can never drift from the statement figure it explains. A
    /// null <paramref name="fromDate"/> means cumulative-through <paramref name="toDate"/> (as-of
    /// semantics); otherwise the window is [fromDate, toDate] inclusive (period semantics). Returns null
    /// when there are no accounts to look at, so callers skip the round-trip entirely.</summary>
    private NoteEntryFilter? BuildEntryFilter(
        Guid tenantId, IReadOnlyList<Guid> chartOfAccountIds, DateOnly? fromDate, DateOnly toDate, Guid? fundId)
    {
        if (chartOfAccountIds.Count == 0)
        {
            return null;
        }

        List<ChartOfAccountId> typedAccountIds =
            chartOfAccountIds.Distinct().Select(id => new ChartOfAccountId(id)).ToList();

        IQueryable<LedgerEntry> entries = db.Set<LedgerEntry>()
            .Where(e => e.TenantId == tenantId && e.BusinessDate <= toDate);

        if (fromDate is DateOnly from)
        {
            entries = entries.Where(e => e.BusinessDate >= from);
        }

        if (fundId is Guid fund)
        {
            Domain.Features.Finance.Funds.FundId typedFundId = new(fund);
            entries = entries.Where(e => e.FundId == typedFundId);
        }

        return new NoteEntryFilter(entries, typedAccountIds);
    }
}
