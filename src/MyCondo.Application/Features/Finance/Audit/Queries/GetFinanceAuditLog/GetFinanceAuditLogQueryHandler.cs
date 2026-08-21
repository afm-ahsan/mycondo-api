using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Audit.DTOs;
using MyCondo.Domain.Features.Finance.Audit;

namespace MyCondo.Application.Features.Finance.Audit.Queries.GetFinanceAuditLog;

public sealed class GetFinanceAuditLogQueryHandler(
    IFinanceAuditLogRepository auditLog,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFinanceAuditLogQuery, List<FinanceAuditLogEntryDto>>
{
    private const int MaxTake = 500;

    public async ValueTask<List<FinanceAuditLogEntryDto>> Handle(GetFinanceAuditLogQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        int take = Math.Clamp(query.Take, 1, MaxTake);
        IReadOnlyList<FinanceAuditLogEntry> entries = await auditLog.GetRecentAsync(tenantId, take, cancellationToken);

        return entries
            .Select(e => new FinanceAuditLogEntryDto(
                e.Id.Value, e.OccurredAtUtc, e.ActorUserId, e.Action, e.TargetType, e.TargetId, e.Metadata, e.CorrelationId))
            .ToList();
    }
}
