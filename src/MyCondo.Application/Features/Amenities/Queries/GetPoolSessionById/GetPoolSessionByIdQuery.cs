using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolSessionById;

public sealed record GetPoolSessionByIdQuery(Guid PoolSessionId) : IRequest<PoolSessionDto>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "facilities.swimming_pool";
}
