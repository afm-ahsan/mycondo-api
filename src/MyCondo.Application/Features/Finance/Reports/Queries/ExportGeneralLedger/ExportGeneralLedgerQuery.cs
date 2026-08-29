using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportGeneralLedger;

/// <summary>No Page/PageSize here, unlike <see cref="GetGeneralLedger.GetGeneralLedgerQuery"/> — export
/// always returns the FULL matching result set for the given filters (see
/// <see cref="ExportGeneralLedgerQueryHandler"/>), never just one page of the on-screen browse.</summary>
public sealed record ExportGeneralLedgerQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? ChartOfAccountId,
    Guid? FundId,
    string? ReferenceType,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, ILifecycleReadOperation;
