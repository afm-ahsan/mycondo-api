using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Identity.Audit.DTOs;
using MyCondo.Application.Features.Identity.Audit.Queries.GetIdentityAuditLog;

namespace MyCondo.Api.Endpoints;

/// <summary>Tenant-scope identity audit trail (mycondo-docs ADR-035) — user/role administrative
/// mutations and privileged-target denials. Structurally mirrors the Finance audit-log endpoint in
/// <see cref="FinanceEndpoints"/>.</summary>
public static class IdentityAuditEndpoints
{
    public static IEndpointRouteBuilder MapIdentityAuditEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder auditLog = app.MapGroup("/api/v1/identity/audit-log").WithTags("Identity");

        auditLog.MapGet("/", async (int take, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetIdentityAuditLogQuery(take == 0 ? 100 : take), ct)))
            .RequirePermission("audit.view")
            .Produces<List<IdentityAuditLogEntryDto>>(StatusCodes.Status200OK);

        return app;
    }
}
