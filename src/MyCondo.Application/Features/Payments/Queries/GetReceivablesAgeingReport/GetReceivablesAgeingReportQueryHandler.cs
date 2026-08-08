using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Payments.Queries.GetReceivablesAgeingReport;

public sealed class GetReceivablesAgeingReportQueryHandler(
    IInvoiceRepository invoices,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetReceivablesAgeingReportQuery, ReceivablesAgeingReportDto>
{
    private static readonly (string Label, int? MinDaysOverdue, int? MaxDaysOverdue)[] Buckets =
    [
        ("Current", null, 0),
        ("1-30 days", 1, 30),
        ("31-60 days", 31, 60),
        ("61-90 days", 61, 90),
        ("91+ days", 91, null),
    ];

    public async ValueTask<ReceivablesAgeingReportDto> Handle(GetReceivablesAgeingReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        IReadOnlyList<AgeingReceivableLine> receivables = await invoices.GetOpenReceivablesAsync(tenantId, buildingId, cancellationToken);

        List<AgeingBucketDto> bucketResults = [];
        decimal grandTotal = 0m;

        foreach ((string label, int? minDaysOverdue, int? maxDaysOverdue) in Buckets)
        {
            List<AgeingReceivableLine> inBucket = receivables
                .Where(r => IsInBucket(asOfDate.DayNumber - r.DueDate.DayNumber, minDaysOverdue, maxDaysOverdue))
                .ToList();

            decimal bucketTotal = inBucket.Sum(r => r.Balance);
            grandTotal += bucketTotal;
            bucketResults.Add(new AgeingBucketDto(label, minDaysOverdue, maxDaysOverdue, inBucket.Count, bucketTotal));
        }

        return new ReceivablesAgeingReportDto(asOfDate, bucketResults, grandTotal);
    }

    private static bool IsInBucket(int daysOverdue, int? minDaysOverdue, int? maxDaysOverdue)
    {
        if (minDaysOverdue is null)
        {
            return daysOverdue <= (maxDaysOverdue ?? 0);
        }

        if (maxDaysOverdue is null)
        {
            return daysOverdue >= minDaysOverdue.Value;
        }

        return daysOverdue >= minDaysOverdue.Value && daysOverdue <= maxDaysOverdue.Value;
    }
}
