using Mediator;
using MyCondo.Application.Features.Finance.Audit.DTOs;

namespace MyCondo.Application.Features.Finance.Audit.Queries.GetFinanceAuditLog;

public sealed record GetFinanceAuditLogQuery(int Take = 100) : IRequest<List<FinanceAuditLogEntryDto>>;
