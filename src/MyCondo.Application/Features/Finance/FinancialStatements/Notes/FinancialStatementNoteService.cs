using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Notes;

/// <inheritdoc cref="IFinancialStatementNoteService"/>
/// <remarks>
/// Every schedule is built by <em>decomposing the same posted ledger entries the primary statement
/// aggregates</em> — restricted to the chart-of-accounts rows belonging to the note's
/// <see cref="FinancialStatementGroup"/>, over the same date window and the same optional fund — and
/// then attributing each slice to a subledger record. That is what makes the schedules historically
/// correct by construction: none of them reads a subledger's current balance, current status, or current
/// outstanding amount to describe a past date.
///
/// Where a slice of ledger activity cannot be traced back to a subledger record, its amount is reported
/// as <see cref="FinancialStatementNote.UnattributedAmount"/> and excluded from
/// <see cref="FinancialStatementNote.ScheduleBalance"/>, producing a real, visible
/// <see cref="FinancialStatementNote.Difference"/> rather than a schedule that silently appears to
/// reconcile.
/// </remarks>
public sealed class FinancialStatementNoteService(IFinancialStatementNoteRepository notes)
    : IFinancialStatementNoteService
{
    /// <summary>Upper bound on returned detail rows per schedule. Totals are always computed from the
    /// complete row set — truncation only limits the payload, never the arithmetic — and is flagged via
    /// <see cref="FinancialStatementNote.IsDetailTruncated"/> with the full count in
    /// <see cref="FinancialStatementNote.TotalRowCount"/>.</summary>
    public const int MaxDetailRows = 500;

    public Task<IReadOnlyList<FinancialStatementNote>> GetAsOfNotesAsync(
        Guid tenantId, DateOnly asOfDate, Guid? fundId,
        IReadOnlyCollection<FinancialStatementNoteKey> keys, CancellationToken cancellationToken) =>
        BuildAsync(
            new NoteWindow(tenantId, null, asOfDate, fundId),
            Requested(keys, IFinancialStatementNoteService.AsOfNoteKeys),
            cancellationToken);

    public Task<IReadOnlyList<FinancialStatementNote>> GetPeriodNotesAsync(
        Guid tenantId, DateOnly startDate, DateOnly endDate, Guid? fundId,
        IReadOnlyCollection<FinancialStatementNoteKey> keys, CancellationToken cancellationToken)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("endDate must not be before startDate.", nameof(endDate));
        }

        return BuildAsync(
            new NoteWindow(tenantId, startDate, endDate, fundId),
            Requested(keys, IFinancialStatementNoteService.PeriodNoteKeys),
            cancellationToken);
    }

    private static IReadOnlyList<FinancialStatementNoteKey> Requested(
        IReadOnlyCollection<FinancialStatementNoteKey> keys, IReadOnlyList<FinancialStatementNoteKey> available) =>
        keys.Count == 0 ? available : available.Where(keys.Contains).ToList();

    private async Task<IReadOnlyList<FinancialStatementNote>> BuildAsync(
        NoteWindow window, IReadOnlyList<FinancialStatementNoteKey> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return [];
        }

        IReadOnlyList<ChartOfAccount> accounts = await notes.GetChartOfAccountsAsync(window.TenantId, cancellationToken);

        List<FinancialStatementNote> built = [];
        foreach (FinancialStatementNoteKey key in keys)
        {
            built.Add(key switch
            {
                FinancialStatementNoteKey.CashAndBank =>
                    await BuildCashAndBankAsync(window, accounts, cancellationToken),
                FinancialStatementNoteKey.FixedDeposits =>
                    await BuildFixedDepositsAsync(window, accounts, cancellationToken),
                FinancialStatementNoteKey.ServiceChargeReceivable =>
                    await BuildFlatBalanceNoteAsync(
                        window, accounts, FinancialStatementNoteKey.ServiceChargeReceivable,
                        FinancialStatementGroup.Receivables,
                        "Service Charge & Other Resident Receivables",
                        ReceivableScopeWarning, cancellationToken),
                FinancialStatementNoteKey.ResidentAdvances =>
                    await BuildFlatBalanceNoteAsync(
                        window, accounts, FinancialStatementNoteKey.ResidentAdvances,
                        FinancialStatementGroup.ResidentAdvances,
                        "Resident Advances", null, cancellationToken),
                FinancialStatementNoteKey.Payables =>
                    await BuildPayablesAsync(window, accounts, cancellationToken),
                FinancialStatementNoteKey.InterestIncome =>
                    await BuildInterestIncomeAsync(window, accounts, cancellationToken),
                FinancialStatementNoteKey.OperatingExpensesByCategory =>
                    await BuildOperatingExpensesAsync(window, accounts, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(keys), key, "Unknown financial statement note."),
            });
        }

        return built;
    }

    // ---------------------------------------------------------------------------------------------
    // Cash & Bank
    // ---------------------------------------------------------------------------------------------

    /// <summary>Rows come from the per-account ledger decomposition, not from any stored account
    /// balance, so an as-of date in the past is reconstructed correctly. Every Cash &amp; Bank general-
    /// ledger account becomes a row — including one with no owning <c>FinancialAccount</c> (the tenant's
    /// default system <c>CashOrBank</c> account is exactly that) — because such a balance is still real
    /// cash, so this schedule reconciles exactly and flags the unlinked accounts as a warning instead.
    /// Zero-balance accounts with activity in the window are kept here (unlike the per-flat schedules):
    /// a bank account that netted to nil is itself information, and the row count is small.</summary>
    private async Task<FinancialStatementNote> BuildCashAndBankAsync(
        NoteWindow window, IReadOnlyList<ChartOfAccount> accounts, CancellationToken cancellationToken)
    {
        const FinancialStatementGroup Group = FinancialStatementGroup.CashAndBank;
        List<ChartOfAccount> groupAccounts = AccountsIn(accounts, Group);

        Dictionary<Guid, decimal> perAccount = await PerAccountBalancesAsync(window, groupAccounts, cancellationToken);
        decimal glBalance = perAccount.Values.Sum();

        IReadOnlyList<NoteFinancialAccountRef> financialAccounts =
            await notes.GetFinancialAccountRefsAsync(window.TenantId, cancellationToken);
        Dictionary<Guid, NoteFinancialAccountRef> byChartOfAccount = financialAccounts
            .GroupBy(a => a.ChartOfAccountId.Value)
            .ToDictionary(g => g.Key, g => g.First());

        List<string> warnings = [];
        List<CashAndBankNoteRow> rows = [];

        foreach (ChartOfAccount account in groupAccounts)
        {
            if (!perAccount.TryGetValue(account.Id.Value, out decimal balance))
            {
                continue;
            }

            if (byChartOfAccount.TryGetValue(account.Id.Value, out NoteFinancialAccountRef? linked))
            {
                rows.Add(new CashAndBankNoteRow(
                    linked.FinancialAccountId.Value, account.Id.Value, account.Code, linked.Name,
                    linked.AccountType, linked.BankName, linked.BranchName, Mask(linked.AccountNumber),
                    linked.FundId?.Value, linked.IsActive, balance, IsUnlinkedGlAccount: false));
            }
            else
            {
                rows.Add(new CashAndBankNoteRow(
                    null, account.Id.Value, account.Code, account.Name, null, null, null, null, null, null,
                    balance, IsUnlinkedGlAccount: true));
            }
        }

        if (rows.Any(r => r.IsUnlinkedGlAccount))
        {
            warnings.Add(
                "One or more Cash & Bank general-ledger accounts are not owned by a Financial Account record " +
                "(typically the tenant's default system Cash/Bank account, which postings that name only the " +
                "CashOrBank role still resolve to). Their balances are included in this schedule and are shown " +
                "under the general-ledger account name.");
        }

        return Compose(
            FinancialStatementNoteKey.CashAndBank, "Cash & Bank Balances", Group, window,
            glBalance, rows.Sum(r => r.Balance), unattributed: 0m, warnings,
            cashAndBankRows: rows.OrderByDescending(r => r.Balance).ToList());
    }

    // ---------------------------------------------------------------------------------------------
    // Fixed Deposits
    // ---------------------------------------------------------------------------------------------

    /// <summary>Per-instrument carrying balance is reconstructed by grouping the Investments/Fixed
    /// Deposits ledger activity by its posting's source reference, so a historical as-of date shows what
    /// each FD was actually carrying then — the FD aggregate's own <c>Principal</c> and <c>Status</c> are
    /// reported alongside as clearly labelled <em>current</em> context only.
    ///
    /// Every posting that moves Fixed Deposit principal names the instrument it moves it for as its source
    /// reference — placement, maturity, void, and renewal capitalization/partial withdrawal
    /// (<c>RenewFixedDepositCommandHandler</c>). Anything that still cannot be traced to an instrument —
    /// notably renewal postings written before those last two carried a reference, which are immutable
    /// posted records and so are never re-attributed retrospectively — is reported as unattributed and
    /// produces a genuine non-zero <see cref="FinancialStatementNote.Difference"/> rather than being
    /// spread across the other rows.
    ///
    /// Accrued interest receivable is a different general-ledger group entirely and is reported per row
    /// without ever being added to the principal total.</summary>
    private async Task<FinancialStatementNote> BuildFixedDepositsAsync(
        NoteWindow window, IReadOnlyList<ChartOfAccount> accounts, CancellationToken cancellationToken)
    {
        const FinancialStatementGroup Group = FinancialStatementGroup.InvestmentsAndFixedDeposits;
        List<ChartOfAccount> groupAccounts = AccountsIn(accounts, Group);

        Dictionary<Guid, decimal> perAccount = await PerAccountBalancesAsync(window, groupAccounts, cancellationToken);
        decimal glBalance = perAccount.Values.Sum();

        IReadOnlyList<NotePostingReferenceLine> principalLines = await notes.GetNotePostingReferenceBalancesAsync(
            window.TenantId, AccountIds(groupAccounts), window.StartDate, window.EndDate, window.FundId,
            cancellationToken);

        LedgerDirection normal = NormalDirectionFor(Group);
        Dictionary<Guid, decimal> byReference = [];
        decimal unattributed = 0m;

        foreach (NotePostingReferenceLine line in principalLines)
        {
            decimal amount = Normalize(normal, line.TotalDebit, line.TotalCredit);
            if (line.ReferenceId is Guid referenceId)
            {
                byReference[referenceId] = byReference.GetValueOrDefault(referenceId) + amount;
            }
            else
            {
                unattributed += amount;
            }
        }

        Dictionary<Guid, decimal> accruedByDeposit =
            await AccruedInterestByDepositAsync(window, accounts, cancellationToken);

        IReadOnlyList<NoteFixedDepositRef> depositRefs = await notes.GetFixedDepositRefsAsync(
            window.TenantId, [.. byReference.Keys.Union(accruedByDeposit.Keys)], cancellationToken);
        Dictionary<Guid, NoteFixedDepositRef> depositsById = depositRefs.ToDictionary(d => d.FixedDepositId.Value);

        Dictionary<Guid, decimal> principalByDeposit = [];
        foreach ((Guid referenceId, decimal amount) in byReference)
        {
            if (depositsById.ContainsKey(referenceId))
            {
                principalByDeposit[referenceId] = principalByDeposit.GetValueOrDefault(referenceId) + amount;
            }
            else
            {
                unattributed += amount;
            }
        }

        List<string> warnings = [];
        List<FixedDepositNoteRow> rows = [];

        foreach (Guid depositId in principalByDeposit.Keys.Union(accruedByDeposit.Keys))
        {
            decimal carrying = principalByDeposit.GetValueOrDefault(depositId);
            decimal accrued = accruedByDeposit.GetValueOrDefault(depositId);
            if (carrying == 0m && accrued == 0m)
            {
                continue;
            }

            if (depositsById.TryGetValue(depositId, out NoteFixedDepositRef? deposit))
            {
                rows.Add(new FixedDepositNoteRow(
                    deposit.FixedDepositId.Value, deposit.CertificateNumber, deposit.BankName, deposit.BranchName,
                    deposit.StartDate, deposit.MaturityDate, deposit.Principal, deposit.InterestRatePercent,
                    deposit.Status, deposit.FundId?.Value, carrying, accrued,
                    IsUnattributed: false, UnattributedReason: null));
            }
            else
            {
                rows.Add(new FixedDepositNoteRow(
                    depositId, null, null, null, null, null, null, null, null, null, carrying, accrued,
                    IsUnattributed: false,
                    UnattributedReason: null));
            }
        }

        decimal scheduleBalance = rows.Sum(r => r.CarryingBalance);

        if (unattributed != 0m)
        {
            rows.Add(new FixedDepositNoteRow(
                null, null, null, null, null, null, null, null, null, null, unattributed, 0m,
                IsUnattributed: true,
                UnattributedReason:
                    "Fixed Deposit ledger activity with no traceable instrument reference. Renewal " +
                    "capitalization and partial-withdrawal postings made before renewal postings carried a " +
                    "source reference cannot be attributed to a certificate; posted ledger records are " +
                    "immutable, so they are reported here rather than retrospectively re-attributed."));

            warnings.Add(
                "Part of the general-ledger Fixed Deposit balance could not be attributed to an individual " +
                "certificate. The statement figure remains the general-ledger balance; the unexplained amount " +
                "is reported as the schedule difference.");
        }

        if (rows.Any(r => r.CertificateNumber is null && !r.IsUnattributed))
        {
            warnings.Add(
                "One or more Fixed Deposit references resolved to no surviving Fixed Deposit record; those rows " +
                "show the general-ledger amount without instrument details.");
        }

        warnings.Add(
            "Principal, accrued interest receivable and interest income are reported separately and never " +
            "combined. Recorded Principal and Status are the instrument's current values — the aggregate keeps " +
            "no history of either, so they are context, not as-of-date figures.");

        return Compose(
            FinancialStatementNoteKey.FixedDeposits, "Fixed Deposits & Investments", Group, window,
            glBalance, scheduleBalance, unattributed, warnings,
            fixedDepositRows: rows
                .OrderByDescending(r => !r.IsUnattributed)
                .ThenByDescending(r => r.CarryingBalance)
                .ToList());
    }

    /// <summary>Accrued FD interest per instrument, as of the note's end date. FD interest postings use
    /// the accrual/receipt id as their source (not the FD id), so this takes the extra hop through
    /// <see cref="IFinancialStatementNoteRepository.GetFixedDepositInterestSourceRefsAsync"/>.</summary>
    private async Task<Dictionary<Guid, decimal>> AccruedInterestByDepositAsync(
        NoteWindow window, IReadOnlyList<ChartOfAccount> accounts, CancellationToken cancellationToken)
    {
        const FinancialStatementGroup Group = FinancialStatementGroup.AccruedInterestReceivable;
        List<ChartOfAccount> groupAccounts = AccountsIn(accounts, Group);
        if (groupAccounts.Count == 0)
        {
            return [];
        }

        IReadOnlyList<NotePostingReferenceLine> lines = await notes.GetNotePostingReferenceBalancesAsync(
            window.TenantId, AccountIds(groupAccounts), window.StartDate, window.EndDate, window.FundId,
            cancellationToken);

        List<Guid> sourceIds = lines.Where(l => l.ReferenceId is not null).Select(l => l.ReferenceId!.Value).ToList();
        if (sourceIds.Count == 0)
        {
            return [];
        }

        IReadOnlyList<NoteFixedDepositInterestSourceRef> sources =
            await notes.GetFixedDepositInterestSourceRefsAsync(window.TenantId, sourceIds, cancellationToken);
        Dictionary<Guid, NoteFixedDepositInterestSourceRef> sourcesById =
            sources.GroupBy(s => s.SourceId).ToDictionary(g => g.Key, g => g.First());

        LedgerDirection normal = NormalDirectionFor(Group);
        Dictionary<Guid, decimal> accrued = [];

        foreach (NotePostingReferenceLine line in lines)
        {
            if (line.ReferenceId is not Guid sourceId ||
                !sourcesById.TryGetValue(sourceId, out NoteFixedDepositInterestSourceRef? source))
            {
                continue;
            }

            decimal amount = Normalize(normal, line.TotalDebit, line.TotalCredit);
            Guid depositId = source.FixedDepositId.Value;
            accrued[depositId] = accrued.GetValueOrDefault(depositId) + amount;
        }

        return accrued;
    }

    // ---------------------------------------------------------------------------------------------
    // Per-flat schedules (Receivables, Resident Advances)
    // ---------------------------------------------------------------------------------------------

    /// <summary>Per-flat balances come straight from the flat-scoped ledger dimension the posting engine
    /// already stamps on <c>ResidentReceivable</c>/<c>ResidentAdvance</c> lines, netted through the
    /// reporting date — so this is a true as-of-date position, not the invoice register's current
    /// outstanding amount, and it settles through payment allocations because that is what the allocation
    /// posting credits. Fully settled flats net to zero and are omitted.</summary>
    private async Task<FinancialStatementNote> BuildFlatBalanceNoteAsync(
        NoteWindow window,
        IReadOnlyList<ChartOfAccount> accounts,
        FinancialStatementNoteKey key,
        FinancialStatementGroup group,
        string title,
        string? scopeWarning,
        CancellationToken cancellationToken)
    {
        List<ChartOfAccount> groupAccounts = AccountsIn(accounts, group);

        Dictionary<Guid, decimal> perAccount = await PerAccountBalancesAsync(window, groupAccounts, cancellationToken);
        decimal glBalance = perAccount.Values.Sum();

        IReadOnlyList<NoteFlatBalanceLine> flatLines = await notes.GetNoteFlatBalancesAsync(
            window.TenantId, AccountIds(groupAccounts), window.StartDate, window.EndDate, window.FundId,
            cancellationToken);

        LedgerDirection normal = NormalDirectionFor(group);
        Dictionary<Guid, decimal> byFlat = [];
        decimal unattributed = 0m;

        foreach (NoteFlatBalanceLine line in flatLines)
        {
            decimal amount = Normalize(normal, line.TotalDebit, line.TotalCredit);
            if (line.FlatId is { } flatId)
            {
                byFlat[flatId.Value] = byFlat.GetValueOrDefault(flatId.Value) + amount;
            }
            else
            {
                unattributed += amount;
            }
        }

        List<Guid> nonZeroFlatIds = byFlat.Where(kv => kv.Value != 0m).Select(kv => kv.Key).ToList();
        IReadOnlyList<NoteFlatRef> flatRefs =
            await notes.GetFlatRefsAsync(window.TenantId, nonZeroFlatIds, cancellationToken);
        Dictionary<Guid, NoteFlatRef> flatsById = flatRefs.ToDictionary(f => f.FlatId.Value);

        List<string> warnings = [];
        List<FlatBalanceNoteRow> rows = [];

        foreach (Guid flatId in nonZeroFlatIds)
        {
            decimal balance = byFlat[flatId];
            if (flatsById.TryGetValue(flatId, out NoteFlatRef? flat))
            {
                rows.Add(new FlatBalanceNoteRow(
                    flatId, flat.FlatNumber, flat.BuildingId, flat.BuildingName, balance, IsUnattributed: false));
            }
            else
            {
                rows.Add(new FlatBalanceNoteRow(flatId, null, null, null, balance, IsUnattributed: false));
            }
        }

        decimal scheduleBalance = rows.Sum(r => r.Balance);

        if (unattributed != 0m)
        {
            rows.Add(new FlatBalanceNoteRow(null, null, null, null, unattributed, IsUnattributed: true));
            warnings.Add(
                "Ledger activity on these accounts carries no flat attribution and could not be assigned to a " +
                "resident position; it is reported as the schedule difference rather than distributed.");
        }

        if (scopeWarning is not null)
        {
            warnings.Add(scopeWarning);
        }

        return Compose(
            key, title, group, window, glBalance, scheduleBalance, unattributed, warnings,
            flatBalanceRows: rows
                .OrderByDescending(r => !r.IsUnattributed)
                .ThenByDescending(r => r.Balance)
                .ToList());
    }

    private const string ReceivableScopeWarning =
        "Regular Service Charge and Additional Charge receivables are not separable: every service-charge " +
        "invoice type posts to the same ServiceChargeIncome role against a single unified ResidentReceivable " +
        "account (ADR-030), and the only distinguishing attribute is the tenant's free-text service-charge rule " +
        "category. This schedule therefore covers all resident receivables classified into the Receivables group.";

    // ---------------------------------------------------------------------------------------------
    // Payables
    // ---------------------------------------------------------------------------------------------

    /// <summary>Outstanding payables are the net of every Accounts Payable movement referencing an
    /// expense through the reporting date — recording, supplier payment, and either void — so a paid or
    /// voided expense nets to zero and is excluded automatically, without this schedule having to
    /// interpret <c>Expense.IsPaid</c> or <c>Status</c> (both of which describe today, not the statement
    /// date).</summary>
    private async Task<FinancialStatementNote> BuildPayablesAsync(
        NoteWindow window, IReadOnlyList<ChartOfAccount> accounts, CancellationToken cancellationToken)
    {
        const FinancialStatementGroup Group = FinancialStatementGroup.AccountsPayable;
        List<ChartOfAccount> groupAccounts = AccountsIn(accounts, Group);

        Dictionary<Guid, decimal> perAccount = await PerAccountBalancesAsync(window, groupAccounts, cancellationToken);
        decimal glBalance = perAccount.Values.Sum();

        (Dictionary<Guid, decimal> byExpense, decimal unattributed, IReadOnlyList<NoteExpenseRef> expenseRefs) =
            await ExpenseAttributionAsync(window, groupAccounts, NormalDirectionFor(Group), cancellationToken);

        Dictionary<Guid, NoteExpenseRef> expensesById = expenseRefs.ToDictionary(e => e.ExpenseId.Value);

        List<string> warnings = [];
        List<PayableNoteRow> rows = [];

        foreach ((Guid expenseId, decimal outstanding) in byExpense)
        {
            if (outstanding == 0m)
            {
                continue;
            }

            NoteExpenseRef expense = expensesById[expenseId];
            rows.Add(new PayableNoteRow(
                expenseId, expense.Description, expense.Payee, expense.ReferenceNumber,
                expense.ExpenseCategoryId?.Value, expense.ExpenseCategoryName, expense.ExpenseTypeName,
                expense.AccountingDate, expense.Amount, expense.Status, outstanding,
                IsUnattributed: false, UnattributedReason: null));
        }

        decimal scheduleBalance = rows.Sum(r => r.OutstandingAmount);

        if (unattributed != 0m)
        {
            rows.Add(new PayableNoteRow(
                null, null, null, null, null, null, null, null, null, null, unattributed,
                IsUnattributed: true,
                UnattributedReason:
                    "Accounts Payable ledger activity that could not be traced to an expense record."));
            warnings.Add(
                "Part of the general-ledger Accounts Payable balance could not be traced to an expense record " +
                "and is reported as the schedule difference.");
        }

        warnings.Add(
            "Outstanding amounts are derived from Accounts Payable ledger movements as of the statement date. " +
            "Expenses paid immediately at recording time never create a payable and therefore never appear here.");

        return Compose(
            FinancialStatementNoteKey.Payables, "Payables", Group, window,
            glBalance, scheduleBalance, unattributed, warnings,
            payableRows: rows
                .OrderByDescending(r => !r.IsUnattributed)
                .ThenByDescending(r => r.OutstandingAmount)
                .ToList());
    }

    // ---------------------------------------------------------------------------------------------
    // Interest Income
    // ---------------------------------------------------------------------------------------------

    /// <summary>Interest <em>recognized</em> is the general-ledger Interest Income activity for the
    /// period, decomposed per instrument; interest <em>received</em> is a separate figure taken from the
    /// FD interest-receipt subledger for receipts whose ledger activity falls in the same window. The
    /// two are reported side by side and never merged — interest is recognized at accrual and received
    /// later, so they are not expected to agree in any given period, and only the recognized figure is
    /// reconciled against the statement.</summary>
    private async Task<FinancialStatementNote> BuildInterestIncomeAsync(
        NoteWindow window, IReadOnlyList<ChartOfAccount> accounts, CancellationToken cancellationToken)
    {
        const FinancialStatementGroup Group = FinancialStatementGroup.InterestIncome;
        List<ChartOfAccount> groupAccounts = AccountsIn(accounts, Group);

        Dictionary<Guid, decimal> perAccount = await PerAccountBalancesAsync(window, groupAccounts, cancellationToken);
        decimal glBalance = perAccount.Values.Sum();

        IReadOnlyList<NotePostingReferenceLine> incomeLines = await notes.GetNotePostingReferenceBalancesAsync(
            window.TenantId, AccountIds(groupAccounts), window.StartDate, window.EndDate, window.FundId,
            cancellationToken);

        IReadOnlyList<NotePostingReferenceLine> receivableLines = await ReceivableActivityLinesAsync(
            window, accounts, cancellationToken);

        List<Guid> sourceIds =
        [
            .. incomeLines.Where(l => l.ReferenceId is not null).Select(l => l.ReferenceId!.Value),
            .. receivableLines.Where(l => l.ReferenceId is not null).Select(l => l.ReferenceId!.Value),
        ];

        IReadOnlyList<NoteFixedDepositInterestSourceRef> sources = sourceIds.Count == 0
            ? []
            : await notes.GetFixedDepositInterestSourceRefsAsync(window.TenantId, sourceIds, cancellationToken);
        Dictionary<Guid, NoteFixedDepositInterestSourceRef> sourcesById =
            sources.GroupBy(s => s.SourceId).ToDictionary(g => g.Key, g => g.First());

        LedgerDirection normal = NormalDirectionFor(Group);
        Dictionary<Guid, decimal> recognizedByDeposit = [];
        decimal unattributed = 0m;

        foreach (NotePostingReferenceLine line in incomeLines)
        {
            decimal amount = Normalize(normal, line.TotalDebit, line.TotalCredit);
            if (line.ReferenceId is Guid sourceId &&
                sourcesById.TryGetValue(sourceId, out NoteFixedDepositInterestSourceRef? source))
            {
                Guid depositId = source.FixedDepositId.Value;
                recognizedByDeposit[depositId] = recognizedByDeposit.GetValueOrDefault(depositId) + amount;
            }
            else
            {
                unattributed += amount;
            }
        }

        Dictionary<Guid, (decimal Gross, decimal Deduction)> receivedByDeposit = [];
        foreach (NotePostingReferenceLine line in receivableLines)
        {
            if (line.ReferenceId is not Guid sourceId ||
                !sourcesById.TryGetValue(sourceId, out NoteFixedDepositInterestSourceRef? source) ||
                !source.IsReceipt)
            {
                continue;
            }

            Guid depositId = source.FixedDepositId.Value;
            (decimal gross, decimal deduction) = receivedByDeposit.GetValueOrDefault(depositId);
            receivedByDeposit[depositId] = (gross + source.GrossAmount, deduction + source.DeductionAmount);
        }

        IReadOnlyList<NoteFixedDepositRef> depositRefs = await notes.GetFixedDepositRefsAsync(
            window.TenantId,
            [.. recognizedByDeposit.Keys.Union(receivedByDeposit.Keys)],
            cancellationToken);
        Dictionary<Guid, NoteFixedDepositRef> depositsById = depositRefs.ToDictionary(d => d.FixedDepositId.Value);

        List<InterestIncomeNoteRow> rows = [];
        foreach (Guid depositId in recognizedByDeposit.Keys.Union(receivedByDeposit.Keys))
        {
            decimal recognized = recognizedByDeposit.GetValueOrDefault(depositId);
            (decimal gross, decimal deduction) = receivedByDeposit.GetValueOrDefault(depositId);
            if (recognized == 0m && gross == 0m && deduction == 0m)
            {
                continue;
            }

            depositsById.TryGetValue(depositId, out NoteFixedDepositRef? deposit);
            rows.Add(new InterestIncomeNoteRow(
                depositId, deposit?.CertificateNumber, deposit?.BankName, recognized, gross, deduction,
                IsUnattributed: false, UnattributedReason: null));
        }

        decimal scheduleBalance = rows.Sum(r => r.InterestRecognized);

        List<string> warnings = [];
        if (unattributed != 0m)
        {
            rows.Add(new InterestIncomeNoteRow(
                null, null, null, unattributed, 0m, 0m,
                IsUnattributed: true,
                UnattributedReason:
                    "Interest income ledger activity that could not be traced to a Fixed Deposit interest accrual — " +
                    "for example non-FD interest posted to the same statement group."));
            warnings.Add(
                "Part of the general-ledger Interest Income figure could not be traced to a Fixed Deposit and is " +
                "reported as the schedule difference.");
        }

        warnings.Add(
            "Interest Recognized is the general-ledger income for the period (recognized at accrual). Interest " +
            "Received and Deducted come from the FD interest-receipt records settled in the same period and are " +
            "shown for context only — they are neither added to, nor reconciled against, the income figure.");

        return Compose(
            FinancialStatementNoteKey.InterestIncome, "Interest Income", Group, window,
            glBalance, scheduleBalance, unattributed, warnings,
            interestIncomeRows: rows
                .OrderByDescending(r => !r.IsUnattributed)
                .ThenByDescending(r => r.InterestRecognized)
                .ToList());
    }

    private async Task<IReadOnlyList<NotePostingReferenceLine>> ReceivableActivityLinesAsync(
        NoteWindow window, IReadOnlyList<ChartOfAccount> accounts, CancellationToken cancellationToken)
    {
        List<ChartOfAccount> groupAccounts =
            AccountsIn(accounts, FinancialStatementGroup.AccruedInterestReceivable);

        return groupAccounts.Count == 0
            ? []
            : await notes.GetNotePostingReferenceBalancesAsync(
                window.TenantId, AccountIds(groupAccounts), window.StartDate, window.EndDate, window.FundId,
                cancellationToken);
    }

    // ---------------------------------------------------------------------------------------------
    // Operating Expenses by category
    // ---------------------------------------------------------------------------------------------

    /// <summary>The breakdown ADR-030 deferred to this task: operating-expense business categories are
    /// tenant-configurable <c>ExpenseCategory</c> data, not chart-of-accounts entries, so the primary
    /// statement necessarily shows one Operating Expenses total. This schedule splits <em>that same
    /// general-ledger total</em> by category — it is built from the ledger activity itself and attributed
    /// through each posting's source expense, so posting status, accounting date, fund, void/reversal
    /// postings and period boundaries are all honoured automatically instead of being re-derived by
    /// summing expense records (which would double-count voids and include never-posted drafts).</summary>
    private async Task<FinancialStatementNote> BuildOperatingExpensesAsync(
        NoteWindow window, IReadOnlyList<ChartOfAccount> accounts, CancellationToken cancellationToken)
    {
        const FinancialStatementGroup Group = FinancialStatementGroup.OperatingExpenses;
        List<ChartOfAccount> groupAccounts = AccountsIn(accounts, Group);

        Dictionary<Guid, decimal> perAccount = await PerAccountBalancesAsync(window, groupAccounts, cancellationToken);
        decimal glBalance = perAccount.Values.Sum();

        (Dictionary<Guid, decimal> byExpense, decimal unattributed, IReadOnlyList<NoteExpenseRef> expenseRefs) =
            await ExpenseAttributionAsync(window, groupAccounts, NormalDirectionFor(Group), cancellationToken);

        Dictionary<Guid, NoteExpenseRef> expensesById = expenseRefs.ToDictionary(e => e.ExpenseId.Value);

        // Keyed by Guid.Empty for expenses whose type predates expense categories — Dictionary cannot
        // take a nullable key, and Guid.Empty is never a real ExpenseCategoryId.
        Dictionary<Guid, (string Name, int Count, decimal Amount)> byCategory = [];
        foreach ((Guid expenseId, decimal amount) in byExpense)
        {
            if (amount == 0m)
            {
                continue;
            }

            NoteExpenseRef expense = expensesById[expenseId];
            Guid categoryKey = expense.ExpenseCategoryId?.Value ?? Guid.Empty;
            string name = expense.ExpenseCategoryName ?? "Uncategorised";

            (string _, int count, decimal total) = byCategory.GetValueOrDefault(categoryKey, (name, 0, 0m));
            byCategory[categoryKey] = (name, count + 1, total + amount);
        }

        List<OperatingExpenseCategoryNoteRow> rows = byCategory
            .Select(kv => new OperatingExpenseCategoryNoteRow(
                kv.Key == Guid.Empty ? null : kv.Key, kv.Value.Name, kv.Value.Count, kv.Value.Amount,
                IsUnattributed: false))
            .ToList();

        decimal scheduleBalance = rows.Sum(r => r.Amount);

        List<string> warnings = [];
        if (unattributed != 0m)
        {
            rows.Add(new OperatingExpenseCategoryNoteRow(
                null, "Unattributed ledger activity", 0, unattributed, IsUnattributed: true));
            warnings.Add(
                "Part of the general-ledger Operating Expenses total could not be traced to an expense record and " +
                "is reported as the schedule difference. The statement figure remains the general-ledger total.");
        }

        if (rows.Any(r => r.ExpenseCategoryId is null && !r.IsUnattributed))
        {
            warnings.Add(
                "Some expenses belong to an expense type created before expense categories existed and are grouped " +
                "as Uncategorised.");
        }

        return Compose(
            FinancialStatementNoteKey.OperatingExpensesByCategory, "Operating Expenses by Category", Group, window,
            glBalance, scheduleBalance, unattributed, warnings,
            operatingExpenseCategoryRows: rows
                .OrderByDescending(r => !r.IsUnattributed)
                .ThenByDescending(r => r.Amount)
                .ToList());
    }

    // ---------------------------------------------------------------------------------------------
    // Shared attribution / helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>Attributes ledger activity on <paramref name="groupAccounts"/> to expense records.
    /// Reversal/void postings reference the <em>original posting id</em> rather than the expense
    /// (<c>VoidExpenseCommandHandler</c>), so anything that does not resolve to an expense on the first
    /// pass is retried through a single bounded posting-reference lookup — two extra round-trips at most,
    /// never one per row.</summary>
    private async Task<(Dictionary<Guid, decimal> ByExpense, decimal Unattributed, IReadOnlyList<NoteExpenseRef> Refs)>
        ExpenseAttributionAsync(
            NoteWindow window, List<ChartOfAccount> groupAccounts, LedgerDirection normal,
            CancellationToken cancellationToken)
    {
        IReadOnlyList<NotePostingReferenceLine> lines = await notes.GetNotePostingReferenceBalancesAsync(
            window.TenantId, AccountIds(groupAccounts), window.StartDate, window.EndDate, window.FundId,
            cancellationToken);

        Dictionary<Guid, decimal> byReference = [];
        decimal unattributed = 0m;

        foreach (NotePostingReferenceLine line in lines)
        {
            decimal amount = Normalize(normal, line.TotalDebit, line.TotalCredit);
            if (line.ReferenceId is Guid referenceId)
            {
                byReference[referenceId] = byReference.GetValueOrDefault(referenceId) + amount;
            }
            else
            {
                unattributed += amount;
            }
        }

        if (byReference.Count == 0)
        {
            return ([], unattributed, []);
        }

        IReadOnlyList<NoteExpenseRef> directRefs =
            await notes.GetExpenseRefsAsync(window.TenantId, [.. byReference.Keys], cancellationToken);
        HashSet<Guid> resolved = directRefs.Select(e => e.ExpenseId.Value).ToHashSet();

        Dictionary<Guid, decimal> byExpense = [];
        List<Guid> unresolved = [];

        foreach ((Guid referenceId, decimal amount) in byReference)
        {
            if (resolved.Contains(referenceId))
            {
                byExpense[referenceId] = byExpense.GetValueOrDefault(referenceId) + amount;
            }
            else
            {
                unresolved.Add(referenceId);
            }
        }

        List<NoteExpenseRef> allRefs = [.. directRefs];

        if (unresolved.Count > 0)
        {
            IReadOnlyList<NotePostingReference> postingRefs =
                await notes.GetPostingReferencesAsync(window.TenantId, unresolved, cancellationToken);
            Dictionary<Guid, Guid> originalReferenceByPosting = postingRefs
                .Where(p => p.ReferenceId is not null)
                .GroupBy(p => p.PostingId.Value)
                .ToDictionary(g => g.Key, g => g.First().ReferenceId!.Value);

            List<Guid> secondHopIds = originalReferenceByPosting.Values.Distinct().Except(resolved).ToList();
            IReadOnlyList<NoteExpenseRef> hopRefs = secondHopIds.Count == 0
                ? []
                : await notes.GetExpenseRefsAsync(window.TenantId, secondHopIds, cancellationToken);

            allRefs.AddRange(hopRefs);
            HashSet<Guid> hopResolved = allRefs.Select(e => e.ExpenseId.Value).ToHashSet();

            foreach (Guid referenceId in unresolved)
            {
                decimal amount = byReference[referenceId];
                if (originalReferenceByPosting.TryGetValue(referenceId, out Guid expenseId) &&
                    hopResolved.Contains(expenseId))
                {
                    byExpense[expenseId] = byExpense.GetValueOrDefault(expenseId) + amount;
                }
                else
                {
                    unattributed += amount;
                }
            }
        }

        return (byExpense, unattributed, allRefs);
    }

    private async Task<Dictionary<Guid, decimal>> PerAccountBalancesAsync(
        NoteWindow window, List<ChartOfAccount> groupAccounts, CancellationToken cancellationToken)
    {
        if (groupAccounts.Count == 0)
        {
            return [];
        }

        IReadOnlyList<NoteAccountBalanceLine> lines = await notes.GetNoteAccountBalancesAsync(
            window.TenantId, AccountIds(groupAccounts), window.StartDate, window.EndDate, window.FundId,
            cancellationToken);

        Dictionary<ChartOfAccountId, LedgerDirection> normalByAccount =
            groupAccounts.ToDictionary(a => a.Id, a => a.NormalBalance);

        Dictionary<Guid, decimal> balances = [];
        foreach (NoteAccountBalanceLine line in lines)
        {
            if (!normalByAccount.TryGetValue(line.ChartOfAccountId, out LedgerDirection normal))
            {
                continue;
            }

            balances[line.ChartOfAccountId.Value] = Normalize(normal, line.TotalDebit, line.TotalCredit);
        }

        return balances;
    }

    private static List<ChartOfAccount> AccountsIn(
        IReadOnlyList<ChartOfAccount> accounts, FinancialStatementGroup group) =>
        accounts.Where(a => a.EffectiveStatementGroup == group).ToList();

    private static List<Guid> AccountIds(List<ChartOfAccount> accounts) =>
        accounts.Select(a => a.Id.Value).ToList();

    /// <summary>The direction a schedule's amounts are signed in. Taken from the group's
    /// <see cref="AccountCategory"/> rather than an individual account, because per-flat and
    /// per-reference decompositions span every account in the group at once. An account whose stored
    /// <c>NormalBalance</c> contradicts its category would make the schedule and the general ledger
    /// disagree — which surfaces as a non-zero difference rather than being silently absorbed.</summary>
    private static LedgerDirection NormalDirectionFor(FinancialStatementGroup group) =>
        FinancialStatementGroupCategories.CategoryOf(group) is AccountCategory.Asset or AccountCategory.Expense
            ? LedgerDirection.Debit
            : LedgerDirection.Credit;

    private static decimal Normalize(LedgerDirection normalBalance, decimal totalDebit, decimal totalCredit) =>
        normalBalance == LedgerDirection.Debit ? totalDebit - totalCredit : totalCredit - totalDebit;

    /// <summary>Shows at most the trailing four characters of a bank/wallet account number — the schedule
    /// identifies which account a balance belongs to without redistributing the full number into a
    /// report that is exported and shared.</summary>
    private static string? Mask(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return null;
        }

        string trimmed = accountNumber.Trim();
        return trimmed.Length <= 4 ? new string('*', trimmed.Length) : $"****{trimmed[^4..]}";
    }

    private static FinancialStatementNote Compose(
        FinancialStatementNoteKey key,
        string title,
        FinancialStatementGroup group,
        NoteWindow window,
        decimal glBalance,
        decimal scheduleBalance,
        decimal unattributed,
        IReadOnlyList<string> warnings,
        List<CashAndBankNoteRow>? cashAndBankRows = null,
        List<FixedDepositNoteRow>? fixedDepositRows = null,
        List<FlatBalanceNoteRow>? flatBalanceRows = null,
        List<PayableNoteRow>? payableRows = null,
        List<InterestIncomeNoteRow>? interestIncomeRows = null,
        List<OperatingExpenseCategoryNoteRow>? operatingExpenseCategoryRows = null)
    {
        int totalRowCount =
            cashAndBankRows?.Count ?? fixedDepositRows?.Count ?? flatBalanceRows?.Count ??
            payableRows?.Count ?? interestIncomeRows?.Count ?? operatingExpenseCategoryRows?.Count ?? 0;

        bool truncated = totalRowCount > MaxDetailRows;
        decimal difference = glBalance - scheduleBalance;

        return new FinancialStatementNote(
            key,
            title,
            window.StartDate is null ? FinancialStatementNoteScope.AsOfDate : FinancialStatementNoteScope.Period,
            group,
            window.StartDate,
            window.EndDate,
            window.FundId,
            glBalance,
            scheduleBalance,
            difference,
            IsReconciled: difference == 0m,
            unattributed,
            totalRowCount,
            truncated,
            warnings,
            Cap(cashAndBankRows),
            Cap(fixedDepositRows),
            Cap(flatBalanceRows),
            Cap(payableRows),
            Cap(interestIncomeRows),
            Cap(operatingExpenseCategoryRows));
    }

    private static List<T>? Cap<T>(List<T>? rows) =>
        rows is null ? null : rows.Count <= MaxDetailRows ? rows : rows.Take(MaxDetailRows).ToList();

    /// <summary>The one date/fund window every schedule in a request shares — <see cref="StartDate"/>
    /// null means as-of (cumulative through <see cref="EndDate"/>).</summary>
    private sealed record NoteWindow(Guid TenantId, DateOnly? StartDate, DateOnly EndDate, Guid? FundId);
}
