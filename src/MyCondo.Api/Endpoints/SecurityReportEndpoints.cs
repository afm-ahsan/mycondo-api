using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.DTOs;
using MyCondo.Application.Features.Security.Queries.GetSecuritySummaryReport;

namespace MyCondo.Api.Endpoints;

public static class SecurityReportEndpoints
{
    public static IEndpointRouteBuilder MapSecurityReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reports = app.MapGroup("/api/v1/reports/security").WithTags("Security Reports");

        reports.MapGet("/summary", async (ISender sender, CancellationToken ct) =>
            {
                SecuritySummaryDto result = await sender.Send(new GetSecuritySummaryReportQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.security.view")
            .Produces<SecuritySummaryDto>(StatusCodes.Status200OK);

        return app;
    }
}
