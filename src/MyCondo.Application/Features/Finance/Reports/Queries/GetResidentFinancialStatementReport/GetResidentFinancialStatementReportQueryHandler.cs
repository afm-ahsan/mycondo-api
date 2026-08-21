using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;

/// <summary>A flat's full ledger-derived statement (charges, payments, running balance) — same
/// opening/running-balance construction as <c>GetAccountLedgerQueryHandler</c>, scoped to one flat's
/// ResidentReceivable activity instead of one tenant-wide account.
///
/// Resident Privacy (non-negotiable): a caller who only holds the self-service
/// <c>finance.report.statement.own.view</c> permission may request only their own flat — enforced via
/// <see cref="IFlatAccessAuthorizer"/>, the same relationship-resolution mechanism
/// <c>GetMyInvoicesQueryHandler</c> uses for the analogous <c>invoice.view.own</c> self-service gate
/// (Phase-3 "Role + Relationship Defense in Depth"). A caller holding the broad
/// <c>finance.report.view</c> permission may request any flat.</summary>
public sealed class GetResidentFinancialStatementReportQueryHandler(
    ILedgerEntryRepository ledgerEntries,
    IFlatRepository flats,
    IFlatAccessAuthorizer flatAccessAuthorizer,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetResidentFinancialStatementReportQuery, ResidentFinancialStatementReportDto>
{
    private const string ViewAnyPermission = "finance.report.view";
    private const string ViewOwnPermission = "finance.report.statement.own.view";

    public async ValueTask<ResidentFinancialStatementReportDto> Handle(
        GetResidentFinancialStatementReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(query.FlatId);
        Flat? flat = await flats.GetByIdAsync(flatId, cancellationToken);
        if (flat is null || flat.TenantId != tenantId)
        {
            throw new NotFoundException("Flat", query.FlatId);
        }

        if (!currentUser.HasPermission(ViewAnyPermission))
        {
            if (currentUser.UserId is not Guid userId)
            {
                throw new ForbiddenException("Authentication required.");
            }

            List<FlatRelationship> relationships =
                await flatAccessAuthorizer.GetActiveRelationshipsAsync(tenantId, userId, cancellationToken);

            bool isOwnFlat = relationships.Any(r => r.FlatId == query.FlatId)
                && currentUser.HasPermissionForBuilding(ViewOwnPermission, flat.BuildingId.Value);

            if (!isOwnFlat)
            {
                throw new ForbiddenException("You may only view your own flat's financial statement.");
            }
        }

        decimal openingBalance = 0m;
        if (query.FromDate is DateOnly fromDate)
        {
            openingBalance = await ledgerEntries.GetReceivableBalanceForFlatBeforeAsync(tenantId, flatId, fromDate, cancellationToken);
        }

        decimal precedingBalance = 0m;
        if (query.Page > 1)
        {
            PagedResult<LedgerEntryWithReference> preceding = await ledgerEntries.SearchForFlatChronologicalAsync(
                tenantId, flatId, query.FromDate, query.ToDate, page: 1, pageSize: (query.Page - 1) * query.PageSize, cancellationToken);

            precedingBalance = preceding.Items.Sum(x => SignedDelta(x.Entry));
        }

        PagedResult<LedgerEntryWithReference> page = await ledgerEntries.SearchForFlatChronologicalAsync(
            tenantId, flatId, query.FromDate, query.ToDate, query.Page, query.PageSize, cancellationToken);

        decimal runningBalance = openingBalance + precedingBalance;
        List<ResidentStatementLineDto> lines = [];
        foreach (LedgerEntryWithReference x in page.Items)
        {
            runningBalance += SignedDelta(x.Entry);
            lines.Add(new ResidentStatementLineDto(
                x.Entry.Id.Value, x.Entry.PostingId.Value, x.Entry.BusinessDate, x.Entry.Description,
                x.ReferenceType, x.ReferenceId, x.Entry.Direction.ToString(), x.Entry.Amount, runningBalance));
        }

        FinanceReportMetadataDto metadata = query.FromDate is DateOnly from && query.ToDate is DateOnly to
            ? FinanceReportMetadataDto.ForPeriod(from, to, $"Flat {flat.FlatNumber}", clock.UtcNow, currentUser.UserId)
            : FinanceReportMetadataDto.ForAsOf(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime), $"Flat {flat.FlatNumber}", clock.UtcNow, currentUser.UserId);

        return new ResidentFinancialStatementReportDto(
            metadata, flat.Id.Value, flat.FlatNumber, openingBalance, lines, runningBalance,
            page.Page, page.PageSize, page.Total);
    }

    /// <summary>ResidentReceivable is Debit-normal (Asset) — a Debit entry increases what the flat owes,
    /// a Credit reduces it.</summary>
    private static decimal SignedDelta(LedgerEntry entry) =>
        entry.Direction == LedgerDirection.Debit ? entry.Amount : -entry.Amount;
}
