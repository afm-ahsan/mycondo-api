using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolDailyUsageReport;

public sealed record GetPoolDailyUsageReportQuery(Guid FacilityId, DateOnly Date) : IRequest<PoolDailyUsageReportDto>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "facilities.reports";
}
