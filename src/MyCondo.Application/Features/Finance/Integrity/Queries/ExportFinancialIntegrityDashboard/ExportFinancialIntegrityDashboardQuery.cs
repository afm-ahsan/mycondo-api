using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Integrity.Queries.ExportFinancialIntegrityDashboard;

public sealed record ExportFinancialIntegrityDashboardQuery(ReportExportFormat Format) : IRequest<ReportExportResult>;
