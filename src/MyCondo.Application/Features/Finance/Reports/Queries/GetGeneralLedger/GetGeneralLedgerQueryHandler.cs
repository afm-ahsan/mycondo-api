using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetGeneralLedger;

/// <summary>The cross-account journal browser <c>FinanceEndpoints.cs</c> flagged as
/// "Reporting-phase scope" at Template 1 — gated by the same <c>finance.journal.view</c> permission
/// reserved for it back then.</summary>
public sealed class GetGeneralLedgerQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetGeneralLedgerQuery, GeneralLedgerReportDto>
{
    public async ValueTask<GeneralLedgerReportDto> Handle(GetGeneralLedgerQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<LedgerActivityLine> page = await reports.GetGeneralLedgerAsync(
            tenantId, query.FromDate, query.ToDate, query.ChartOfAccountId, query.FundId, query.ReferenceType,
            query.Page, query.PageSize, ascending: false, cancellationToken);

        List<GeneralLedgerLineDto> lines = page.Items
            .Select(l => new GeneralLedgerLineDto(
                l.EntryId.Value, l.PostingId.Value, l.BusinessDate, l.Description, l.ReferenceType, l.ReferenceId,
                l.ChartOfAccountId?.Value, l.ChartOfAccountCode, l.ChartOfAccountName, l.FlatId?.Value, l.FundId?.Value,
                l.Direction.ToString(), l.Amount))
            .ToList();

        string scopeSummary = query.ChartOfAccountId is Guid
            ? "Single account"
            : "Tenant (all accounts)";

        FinanceReportMetadataDto metadata = query.FromDate is DateOnly from && query.ToDate is DateOnly to
            ? FinanceReportMetadataDto.ForPeriod(from, to, scopeSummary, clock.UtcNow, currentUser.UserId)
            : FinanceReportMetadataDto.ForAsOf(DateOnly.FromDateTime(clock.UtcNow.UtcDateTime), scopeSummary, clock.UtcNow, currentUser.UserId);

        return new GeneralLedgerReportDto(metadata, lines, page.Page, page.PageSize, page.Total);
    }
}
