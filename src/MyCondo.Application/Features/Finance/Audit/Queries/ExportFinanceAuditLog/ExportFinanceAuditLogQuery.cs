using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.Audit.Queries.ExportFinanceAuditLog;

public sealed record ExportFinanceAuditLogQuery(int Take, ReportExportFormat Format) : IRequest<ReportExportResult>, ILifecycleReadOperation;
