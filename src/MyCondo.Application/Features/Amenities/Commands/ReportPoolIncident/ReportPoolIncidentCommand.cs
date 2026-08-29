using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.ReportPoolIncident;

public sealed record ReportPoolIncidentCommand(
    Guid FacilityId,
    Guid? PoolSessionId,
    DateTimeOffset OccurredAtUtc,
    string Description,
    string Severity,
    string? ActionTaken
) : IRequest<PoolIncidentDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "facilities.swimming_pool";
}
