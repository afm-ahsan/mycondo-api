using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Common.Services;

/// <summary>
/// Seeds the 6 system accounts backing the pre-existing <see cref="LedgerAccountType"/> posting roles
/// (ADR-027), plus their <see cref="AccountMapping"/> — the minimum a tenant needs before any Billing/
/// Payments/Amenities/Utilities posting call site can succeed through
/// <c>Application.Features.Finance.Services.IFinancialPostingService</c>. Reconciles by
/// <see cref="ChartOfAccount.Code"/>/<see cref="AccountMapping.PostingRole"/> — safe to call on every
/// tenant-bootstrap run, same pattern as <see cref="ExpenseTypeCatalogueSeeder"/>.
/// </summary>
public sealed class FinanceChartOfAccountSeeder(
    IChartOfAccountRepository chartOfAccounts,
    IAccountMappingRepository accountMappings,
    ILogger<FinanceChartOfAccountSeeder> logger
) : IFinanceChartOfAccountSeeder
{
    private static readonly (LedgerAccountType Role, string Code, string Name, AccountCategory Category, LedgerDirection NormalBalance, FinancialStatementGroup StatementGroup)[] SystemAccounts =
    [
        (LedgerAccountType.CashOrBank, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit, FinancialStatementGroup.CashAndBank),
        (LedgerAccountType.ResidentReceivable, "1100", "Resident Receivable", AccountCategory.Asset, LedgerDirection.Debit, FinancialStatementGroup.Receivables),
        (LedgerAccountType.RefundableDepositsHeld, "2100", "Refundable Deposits Held", AccountCategory.Liability, LedgerDirection.Credit, FinancialStatementGroup.OtherLiabilities),
        // Added by the Billing↔Finance integration template (ADR-027 follow-up) — see
        // LedgerAccountType.ResidentAdvance's doc comment.
        (LedgerAccountType.ResidentAdvance, "2200", "Resident Advance / Unallocated Credit", AccountCategory.Liability, LedgerDirection.Credit, FinancialStatementGroup.ResidentAdvances),
        (LedgerAccountType.OpeningBalanceEquity, "3900", "Opening Balance Equity", AccountCategory.Equity, LedgerDirection.Credit, FinancialStatementGroup.AccumulatedSurplus),
        (LedgerAccountType.AssociationRevenue, "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit, FinancialStatementGroup.OtherIncome),
        // Added by the Billing↔Finance integration template — differentiated income per charge type;
        // the receivable side stays unified on ResidentReceivable (see LedgerPosting.Create).
        (LedgerAccountType.ServiceChargeIncome, "4010", "Service Charge Income", AccountCategory.Income, LedgerDirection.Credit, FinancialStatementGroup.ServiceChargeIncome),
        (LedgerAccountType.GasRecoveryIncome, "4020", "Gas Recovery Income", AccountCategory.Income, LedgerDirection.Credit, FinancialStatementGroup.UtilityGasIncome),
        (LedgerAccountType.FineIncome, "4030", "Fine Income", AccountCategory.Income, LedgerDirection.Credit, FinancialStatementGroup.FineIncome),
        (LedgerAccountType.AdjustmentsAndWaivers, "4900", "Adjustments and Waivers", AccountCategory.Income, LedgerDirection.Debit, FinancialStatementGroup.OtherIncome),
        // Added by Template 3 (Expense Accounting Integration) — see LedgerAccountType.OperatingExpense/
        // AccountsPayable's doc comments.
        (LedgerAccountType.AccountsPayable, "2300", "Accounts Payable", AccountCategory.Liability, LedgerDirection.Credit, FinancialStatementGroup.AccountsPayable),
        (LedgerAccountType.OperatingExpense, "5000", "Operating Expenses", AccountCategory.Expense, LedgerDirection.Debit, FinancialStatementGroup.OperatingExpenses),
        // Added by Template 4 (Banking, Fixed Deposits & Interest) — see LedgerAccountType.FixedDeposit/
        // InterestReceivable/FDInterestIncome/InterestDeductionExpense's doc comments.
        (LedgerAccountType.FixedDeposit, "1200", "Fixed Deposits", AccountCategory.Asset, LedgerDirection.Debit, FinancialStatementGroup.InvestmentsAndFixedDeposits),
        (LedgerAccountType.InterestReceivable, "1300", "FD Interest Receivable", AccountCategory.Asset, LedgerDirection.Debit, FinancialStatementGroup.AccruedInterestReceivable),
        (LedgerAccountType.FDInterestIncome, "4040", "FD Interest Income", AccountCategory.Income, LedgerDirection.Credit, FinancialStatementGroup.InterestIncome),
        (LedgerAccountType.InterestDeductionExpense, "5100", "Interest Deduction / Withholding", AccountCategory.Expense, LedgerDirection.Debit, FinancialStatementGroup.InterestExpense),
    ];

    public async Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        int accountsCreated = 0;
        int mappingsCreated = 0;
        int accountsReclassified = 0;

        IReadOnlyList<ChartOfAccount> existingAccounts = await chartOfAccounts.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<string, ChartOfAccount> existingByCode = existingAccounts.ToDictionary(a => a.Code);

        foreach ((LedgerAccountType role, string code, string name, AccountCategory category, LedgerDirection normalBalance, FinancialStatementGroup statementGroup) in SystemAccounts)
        {
            if (!existingByCode.TryGetValue(code, out ChartOfAccount? account))
            {
                account = ChartOfAccount.Create(
                    tenantId, code, name, category, normalBalance, parentAccountId: null, isSystemAccount: true,
                    statementGroup: statementGroup);
                chartOfAccounts.Add(account);
                accountsCreated++;

                AccountMapping mapping = AccountMapping.Create(tenantId, role.ToString(), account.Id);
                accountMappings.Add(mapping);
                mappingsCreated++;
                continue;
            }

            if (account.StatementGroup != statementGroup)
            {
                // Backfill for a tenant seeded before this system account's classification existed —
                // reconciled by Code, same idempotent pattern as the account/mapping reconciliation below.
                account.Reclassify(statementGroup);
                accountsReclassified++;
            }

            AccountMapping? existingMapping = await accountMappings.GetByRoleAsync(tenantId, role.ToString(), cancellationToken);
            if (existingMapping is null)
            {
                // The account already exists (e.g. re-run after a partial failure) but its mapping
                // doesn't.
                accountMappings.Add(AccountMapping.Create(tenantId, role.ToString(), account.Id));
                mappingsCreated++;
            }
        }

        logger.LogInformation(
            "[DatabaseSeed] Finance chart of accounts for tenant {TenantId}: {ExpectedCount} expected, " +
            "{AccountsCreated} account(s) created, {MappingsCreated} mapping(s) created, " +
            "{AccountsReclassified} account(s) reclassified",
            tenantId, SystemAccounts.Length, accountsCreated, mappingsCreated, accountsReclassified);
    }
}
