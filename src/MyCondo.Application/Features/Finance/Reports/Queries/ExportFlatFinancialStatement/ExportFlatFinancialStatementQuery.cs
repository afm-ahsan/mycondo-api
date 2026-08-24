using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFlatFinancialStatement;

/// <summary>No Page/PageSize here, unlike
/// <see cref="GetFlatFinancialStatement.GetFlatFinancialStatementQuery"/> — export always returns the
/// FULL statement for the flat and date range (see
/// <see cref="ExportFlatFinancialStatementQueryHandler"/>), never just one page of the on-screen
/// browse.</summary>
public sealed record ExportFlatFinancialStatementQuery(
    Guid FlatId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
