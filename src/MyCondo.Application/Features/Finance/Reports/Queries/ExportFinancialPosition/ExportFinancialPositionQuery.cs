using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialPosition;

public sealed record ExportFinancialPositionQuery(DateOnly? AsOfDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>;
