using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.ResolveBreakdown;

public sealed record ResolveBreakdownCommand(
    Guid GeneratorBreakdownRecordId,
    string Resolution,
    decimal? Cost,
    DateTimeOffset DowntimeEndUtc
) : IRequest<GeneratorBreakdownRecordDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "operations.generator";
}
