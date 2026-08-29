using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookingRevenueReport;

public sealed record GetBookingRevenueReportQuery(
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<IReadOnlyList<BookingRevenueReportLineDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "facilities.reports";
}
