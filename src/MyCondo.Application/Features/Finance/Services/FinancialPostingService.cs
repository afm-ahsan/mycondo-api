using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.AccountingPeriods.Exceptions;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.AccountMappings.Exceptions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.ChartOfAccounts.Exceptions;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Services;

public sealed class FinancialPostingService(
    IAccountMappingRepository accountMappings,
    IChartOfAccountRepository chartOfAccounts,
    IAccountingPeriodRepository accountingPeriods,
    ILedgerPostingRepository ledgerPostings,
    ILedgerEntryRepository ledgerEntries,
    IClock clock,
    ILogger<FinancialPostingService> logger
) : IFinancialPostingService
{
    public async Task<FinancialPostingResult> PostAsync(FinancialPostingRequest request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
        {
            throw new ArgumentException("A posting requires at least one line.", nameof(request));
        }

        if (request.SourceId is Guid sourceId)
        {
            bool duplicate = await ledgerPostings.ExistsAsync(
                request.TenantId, request.PostingPurpose, sourceId, cancellationToken);
            if (duplicate)
            {
                throw new ConflictException(
                    $"A posting already exists for purpose '{request.PostingPurpose}' and source {sourceId} — duplicate execution rejected.");
            }
        }

        Dictionary<ChartOfAccountId, bool> validatedExplicitAccounts = [];
        Dictionary<LedgerAccountType, ChartOfAccountId> resolvedRoleAccounts = [];
        List<ChartOfAccountId> resolvedLineAccounts = new(request.Lines.Count);
        foreach (FinancialPostingLine line in request.Lines)
        {
            if (line.ExplicitAccountId is ChartOfAccountId explicitAccountId)
            {
                if (!validatedExplicitAccounts.ContainsKey(explicitAccountId))
                {
                    await ValidateExplicitAccountAsync(request.TenantId, explicitAccountId, cancellationToken);
                    validatedExplicitAccounts[explicitAccountId] = true;
                }

                resolvedLineAccounts.Add(explicitAccountId);
                continue;
            }

            if (!resolvedRoleAccounts.TryGetValue(line.Role, out ChartOfAccountId roleAccountId))
            {
                ChartOfAccountId? accountId = await accountMappings.ResolveAccountIdAsync(
                    request.TenantId, line.Role.ToString(), cancellationToken);
                if (accountId is null)
                {
                    throw new MissingAccountMappingException(request.TenantId, line.Role.ToString());
                }

                roleAccountId = accountId.Value;
                resolvedRoleAccounts[line.Role] = roleAccountId;
            }

            resolvedLineAccounts.Add(roleAccountId);
        }

        AccountingPeriod? period = await accountingPeriods.FindCoveringAsync(
            request.TenantId, request.BusinessDate, cancellationToken);
        if (period is not null)
        {
            if (period.Status == AccountingPeriodStatus.Closed)
            {
                throw new AccountingPeriodClosedException(period.Id, request.BusinessDate);
            }

            if (period.Status == AccountingPeriodStatus.SoftClosed && !request.IsPrivilegedAdjustment)
            {
                throw new AccountingPeriodSoftClosedException(period.Id, request.BusinessDate);
            }
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        List<LedgerLine> lines = request.Lines
            .Select(l => new LedgerLine(l.Role, l.FlatId, l.Direction, l.Amount, l.LineDescription ?? request.Description))
            .ToList();

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            request.TenantId, request.BusinessDate, request.Description, request.PostingPurpose,
            request.SourceId, lines, nowUtc);

        foreach ((LedgerEntry entry, ChartOfAccountId accountId) in entries.Zip(resolvedLineAccounts))
        {
            entry.SetFinanceDimensions(accountId, request.FundId, period?.Id);
        }

        ledgerPostings.Add(posting);
        ledgerEntries.AddRange(entries);

        logger.LogInformation(
            "Posted journal entry {PostingId} for tenant {TenantId}, purpose {PostingPurpose}, {LineCount} line(s)",
            posting.Id, request.TenantId, request.PostingPurpose, entries.Count);

        return new FinancialPostingResult(posting, entries);
    }

    /// <summary>Defense-in-depth for <see cref="FinancialPostingLine.ExplicitAccountId"/>: the posting
    /// engine is the single centralized entry point (ADR-027) and must not trust a caller-supplied
    /// account id at face value, even though today's only callers (Financial Account commands) already
    /// validate ownership before constructing the request.</summary>
    private async Task ValidateExplicitAccountAsync(Guid tenantId, ChartOfAccountId accountId, CancellationToken cancellationToken)
    {
        ChartOfAccount? account = await chartOfAccounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            throw new InvalidExplicitPostingAccountException(tenantId, accountId, "account does not exist.");
        }

        if (account.TenantId != tenantId)
        {
            throw new InvalidExplicitPostingAccountException(tenantId, accountId, "account belongs to a different tenant.");
        }

        if (!account.IsActive)
        {
            throw new InvalidExplicitPostingAccountException(tenantId, accountId, "account is inactive.");
        }
    }
}
