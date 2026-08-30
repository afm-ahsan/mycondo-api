using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Identity.Audit.DTOs;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Identity.Audit.Queries.GetIdentityAuditLog;

public sealed class GetIdentityAuditLogQueryHandler(
    IIdentityAuditLogRepository auditLog,
    IUserRepository users,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetIdentityAuditLogQuery, List<IdentityAuditLogEntryDto>>
{
    private const int MaxTake = 500;
    private const string SystemActor = "System";
    private const string UnknownActor = "Unknown user";

    public async ValueTask<List<IdentityAuditLogEntryDto>> Handle(
        GetIdentityAuditLogQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        int take = Math.Clamp(query.Take, 1, MaxTake);
        IReadOnlyList<IdentityAuditLogEntry> entries = await auditLog.GetRecentAsync(tenantId, take, cancellationToken);

        List<UserId> actorIds = entries
            .Where(e => e.ActorUserId is not null)
            .Select(e => new UserId(e.ActorUserId!.Value))
            .Distinct()
            .ToList();
        Dictionary<UserId, string> actorNamesById = (await users.GetByIdsAsync(tenantId, actorIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u.FullName);

        return entries
            .Select(e => new IdentityAuditLogEntryDto(
                e.Id.Value, e.OccurredAtUtc, e.ActorUserId,
                e.ActorUserId is null
                    ? SystemActor
                    : actorNamesById.GetValueOrDefault(new UserId(e.ActorUserId.Value), UnknownActor),
                e.Action, e.TargetType, e.TargetId, e.Metadata, e.CorrelationId))
            .ToList();
    }
}
