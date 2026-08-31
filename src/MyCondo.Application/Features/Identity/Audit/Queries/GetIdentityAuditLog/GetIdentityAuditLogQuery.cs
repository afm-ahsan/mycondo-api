using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Identity.Audit.DTOs;

namespace MyCondo.Application.Features.Identity.Audit.Queries.GetIdentityAuditLog;

public sealed record GetIdentityAuditLogQuery(int Take = 100) : IRequest<List<IdentityAuditLogEntryDto>>, ILifecycleReadOperation;
