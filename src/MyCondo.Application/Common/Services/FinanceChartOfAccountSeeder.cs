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
    private static readonly (LedgerAccountType Role, string Code, string Name, AccountCategory Category, LedgerDirection NormalBalance)[] SystemAccounts =
    [
        (LedgerAccountType.CashOrBank, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit),
        (LedgerAccountType.ResidentReceivable, "1100", "Resident Receivable", AccountCategory.Asset, LedgerDirection.Debit),
        (LedgerAccountType.RefundableDepositsHeld, "2100", "Refundable Deposits Held", AccountCategory.Liability, LedgerDirection.Credit),
        (LedgerAccountType.OpeningBalanceEquity, "3900", "Opening Balance Equity", AccountCategory.Equity, LedgerDirection.Credit),
        (LedgerAccountType.AssociationRevenue, "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit),
        (LedgerAccountType.AdjustmentsAndWaivers, "4900", "Adjustments and Waivers", AccountCategory.Income, LedgerDirection.Debit),
    ];

    public async Task SeedAsync(Guid tenantId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        int accountsCreated = 0;
        int mappingsCreated = 0;

        foreach ((LedgerAccountType role, string code, string name, AccountCategory category, LedgerDirection normalBalance) in SystemAccounts)
        {
            bool accountExists = await chartOfAccounts.ExistsForCodeAsync(tenantId, code, cancellationToken);
            if (!accountExists)
            {
                ChartOfAccount account = ChartOfAccount.Create(
                    tenantId, code, name, category, normalBalance, parentAccountId: null, isSystemAccount: true);
                chartOfAccounts.Add(account);
                accountsCreated++;

                AccountMapping mapping = AccountMapping.Create(tenantId, role.ToString(), account.Id);
                accountMappings.Add(mapping);
                mappingsCreated++;
                continue;
            }

            AccountMapping? existingMapping = await accountMappings.GetByRoleAsync(tenantId, role.ToString(), cancellationToken);
            if (existingMapping is null)
            {
                // The account already exists (e.g. re-run after a partial failure) but its mapping
                // doesn't — look it up by code rather than re-creating the account.
                IReadOnlyList<ChartOfAccount> all = await chartOfAccounts.GetAllForTenantAsync(tenantId, cancellationToken);
                ChartOfAccount account = all.First(a => a.Code == code);
                accountMappings.Add(AccountMapping.Create(tenantId, role.ToString(), account.Id));
                mappingsCreated++;
            }
        }

        logger.LogInformation(
            "[DatabaseSeed] Finance chart of accounts for tenant {TenantId}: {ExpectedCount} expected, " +
            "{AccountsCreated} account(s) created, {MappingsCreated} mapping(s) created",
            tenantId, SystemAccounts.Length, accountsCreated, mappingsCreated);
    }
}
