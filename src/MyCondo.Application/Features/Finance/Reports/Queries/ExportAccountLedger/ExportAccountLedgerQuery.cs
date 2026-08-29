using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportAccountLedger;

/// <summary>No Page/PageSize here, unlike <see cref="GetAccountLedger.GetAccountLedgerQuery"/> — export
/// always returns the FULL matching result set for the account and date range (see
/// <see cref="ExportAccountLedgerQueryHandler"/>), never just one page of the on-screen browse.</summary>
public sealed record ExportAccountLedgerQuery(
    Guid ChartOfAccountId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, ILifecycleReadOperation;
