using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.ExportOrganizationBillingHistory;
using MyCondo.Application.Features.Platform.Queries.ExportPlatformBillingSummary;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationBillingHistory;
using MyCondo.Application.Features.Platform.Queries.GetOutstandingDues;
using MyCondo.Application.Features.Platform.Queries.GetPlatformBillingSummary;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoiceById;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoicePayments;
using MyCondo.Application.Features.Platform.Queries.ListSubscriptionInvoices;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Platform-scope subscription-invoice/outstanding-dues/collections read model (ADR-034 Tasks 14D/14L)
/// — read-only, no lifecycle mutation. Reuses the same "platform.subscription.read" permission the
/// existing Task 13A subscription-detail read already gates on (see <c>PlatformOrganizationEndpoints</c>);
/// ADR-034's reserved "platform.billing.read"/"platform.payment.read" permission names were never
/// actually seeded by Tasks 14A-14C, so this task does not introduce them either. The Task 14L reports
/// below reuse this same permission — this is CondoBD Platform Subscription Billing/Collections
/// reporting, never exposed through the Task 14I tenant billing-resolution contract.
/// </summary>
public static class PlatformBillingEndpoints
{
    public static IEndpointRouteBuilder MapPlatformBillingEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/platform/billing").WithTags("Platform Billing");

        group.MapGet("/invoices", async (
                int page, int pageSize, Guid? organizationId, string? status, bool? overdueOnly,
                DateOnly? dueDateFrom, DateOnly? dueDateTo, ISender sender, CancellationToken ct) =>
            {
                PagedResult<PlatformSubscriptionInvoiceListItemDto> result = await sender.Send(
                    new ListSubscriptionInvoicesQuery(
                        page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize, organizationId, status, overdueOnly,
                        dueDateFrom, dueDateTo),
                    ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.subscription.read")
            .Produces<PagedResult<PlatformSubscriptionInvoiceListItemDto>>(StatusCodes.Status200OK);

        group.MapGet("/invoices/{invoiceId:guid}", async (Guid invoiceId, ISender sender, CancellationToken ct) =>
            {
                PlatformSubscriptionInvoiceDetailDto result =
                    await sender.Send(new GetSubscriptionInvoiceByIdQuery(invoiceId), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.subscription.read")
            .Produces<PlatformSubscriptionInvoiceDetailDto>(StatusCodes.Status200OK);

        group.MapGet("/invoices/{invoiceId:guid}/payments", async (Guid invoiceId, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<PlatformSubscriptionPaymentDto> result =
                    await sender.Send(new GetSubscriptionInvoicePaymentsQuery(invoiceId), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.subscription.read")
            .Produces<IReadOnlyList<PlatformSubscriptionPaymentDto>>(StatusCodes.Status200OK);

        group.MapGet("/outstanding", async (
                Guid? organizationId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<PlatformOrganizationOutstandingDto> result = await sender.Send(
                    new GetOutstandingDuesQuery(organizationId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize),
                    ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.subscription.read")
            .Produces<PagedResult<PlatformOrganizationOutstandingDto>>(StatusCodes.Status200OK);

        group.MapGet("/reports/summary", async (
                Guid? organizationId, DateOnly? dateFrom, DateOnly? dateTo, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<PlatformBillingSummaryCurrencyDto> result = await sender.Send(
                    new GetPlatformBillingSummaryQuery(organizationId, dateFrom, dateTo), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.subscription.read")
            .Produces<IReadOnlyList<PlatformBillingSummaryCurrencyDto>>(StatusCodes.Status200OK);

        group.MapGet("/reports/summary/export", async (
                Guid? organizationId, DateOnly? dateFrom, DateOnly? dateTo, string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportPlatformBillingSummaryQuery(organizationId, dateFrom, dateTo, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePlatformPermission("platform.subscription.read");

        group.MapGet("/reports/organization-history", async (
                Guid organizationId, DateOnly? dateFrom, DateOnly? dateTo, int page, int pageSize,
                ISender sender, CancellationToken ct) =>
            {
                PagedResult<OrganizationBillingHistoryEventDto> result = await sender.Send(
                    new GetOrganizationBillingHistoryQuery(
                        organizationId, dateFrom, dateTo, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize),
                    ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.subscription.read")
            .Produces<PagedResult<OrganizationBillingHistoryEventDto>>(StatusCodes.Status200OK);

        group.MapGet("/reports/organization-history/export", async (
                Guid organizationId, DateOnly? dateFrom, DateOnly? dateTo, string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportOrganizationBillingHistoryQuery(organizationId, dateFrom, dateTo, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePlatformPermission("platform.subscription.read");

        return app;
    }
}
