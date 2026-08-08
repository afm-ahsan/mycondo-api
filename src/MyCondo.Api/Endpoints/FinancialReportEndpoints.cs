using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetFinancialSummaryReport;
using MyCondo.Application.Features.Payments.Queries.GetReceivablesAgeingReport;

namespace MyCondo.Api.Endpoints;

public static class FinancialReportEndpoints
{
    public static IEndpointRouteBuilder MapFinancialReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reports = app.MapGroup("/api/v1/reports/financial").WithTags("Financial Reports");

        reports.MapGet("/summary", async (Guid? buildingId, DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                FinancialSummaryDto result = await sender.Send(new GetFinancialSummaryReportQuery(buildingId, fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.financial.view")
            .Produces<FinancialSummaryDto>(StatusCodes.Status200OK);

        reports.MapGet("/receivables-ageing", async (Guid? buildingId, DateOnly? asOfDate, ISender sender, CancellationToken ct) =>
            {
                ReceivablesAgeingReportDto result = await sender.Send(new GetReceivablesAgeingReportQuery(buildingId, asOfDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.financial.view")
            .Produces<ReceivablesAgeingReportDto>(StatusCodes.Status200OK);

        return app;
    }
}
