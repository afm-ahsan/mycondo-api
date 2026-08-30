using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Utilities.Queries.ExportConsumptionHistoryReport;

public sealed record ExportConsumptionHistoryReportQuery(
    Guid MeterId,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, ILifecycleReadOperation;
