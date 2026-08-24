using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportResidentFinancialStatementReport;

/// <summary>No Page/PageSize here, unlike
/// <see cref="GetResidentFinancialStatementReport.GetResidentFinancialStatementReportQuery"/> — export
/// always returns the FULL statement for the flat and date range (see
/// <see cref="ExportResidentFinancialStatementReportQueryHandler"/>), never just one page of the
/// on-screen browse. <see cref="FlatId"/> is whichever flat the caller is asking about; the underlying
/// <c>GetResidentFinancialStatementReportQueryHandler</c> still enforces the Resident Privacy two-tier
/// check (self-service callers may only request their own flat) exactly as it does for the on-screen
/// query — see the route registration in <c>FinanceReportEndpoints</c> for why this endpoint is gated by
/// authentication only, not a single permission string.</summary>
public sealed record ExportResidentFinancialStatementReportQuery(
    Guid FlatId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
