using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Payments.Queries.GetFinancialSummaryReport;

public sealed class GetFinancialSummaryReportQueryHandler(
    IInvoiceRepository invoices,
    IPaymentRepository payments,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFinancialSummaryReportQuery, FinancialSummaryDto>
{
    public async ValueTask<FinancialSummaryDto> Handle(GetFinancialSummaryReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        DateOnly asOfDate = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        InvoiceFinancialAggregate invoiceAggregate = await invoices.GetFinancialAggregateAsync(
            tenantId, buildingId, query.FromDate, query.ToDate, asOfDate, cancellationToken);

        decimal totalCollected = await payments.GetTotalCollectedAsync(
            tenantId, buildingId, query.FromDate, query.ToDate, cancellationToken);

        return new FinancialSummaryDto(
            query.FromDate,
            query.ToDate,
            asOfDate,
            invoiceAggregate.TotalBilled,
            totalCollected,
            invoiceAggregate.TotalOutstanding,
            invoiceAggregate.UnpaidInvoiceCount,
            invoiceAggregate.PartiallyPaidInvoiceCount,
            invoiceAggregate.OverdueInvoiceCount);
    }
}
