using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.AccountingPeriods.Exceptions;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.AccountMappings.Exceptions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Services;

public sealed class FinancialPostingService(
    IAccountMappingRepository accountMappings,
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

        Dictionary<LedgerAccountType, ChartOfAccountId> resolvedAccounts = [];
        foreach (LedgerAccountType role in request.Lines.Select(l => l.Role).Distinct())
        {
            ChartOfAccountId? accountId = await accountMappings.ResolveAccountIdAsync(
                request.TenantId, role.ToString(), cancellationToken);
            if (accountId is null)
            {
                throw new MissingAccountMappingException(request.TenantId, role.ToString());
            }

            resolvedAccounts[role] = accountId.Value;
        }

        AccountingPeriod? period = await accountingPeriods.FindCoveringAsync(
            request.TenantId, request.BusinessDate, cancellationToken);
        if (period is not null && period.Status == AccountingPeriodStatus.Closed)
        {
            throw new AccountingPeriodClosedException(period.Id, request.BusinessDate);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        List<LedgerLine> lines = request.Lines
            .Select(l => new LedgerLine(l.Role, l.FlatId, l.Direction, l.Amount, l.LineDescription ?? request.Description))
            .ToList();

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            request.TenantId, request.BusinessDate, request.Description, request.PostingPurpose,
            request.SourceId, lines, nowUtc);

        foreach ((LedgerEntry entry, FinancialPostingLine line) in entries.Zip(request.Lines))
        {
            entry.SetFinanceDimensions(resolvedAccounts[line.Role], request.FundId, period?.Id);
        }

        ledgerPostings.Add(posting);
        ledgerEntries.AddRange(entries);

        logger.LogInformation(
            "Posted journal entry {PostingId} for tenant {TenantId}, purpose {PostingPurpose}, {LineCount} line(s)",
            posting.Id, request.TenantId, request.PostingPurpose, entries.Count);

        return new FinancialPostingResult(posting, entries);
    }
}
