using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFundPosition;

public sealed record ExportFundPositionQuery(DateOnly? AsOfDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>;
