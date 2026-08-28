using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.CheckOutPoolSession;

public sealed record CheckOutPoolSessionCommand(Guid PoolSessionId) : IRequest<PoolSessionDto>, IRequiresFeature
{
    public string FeatureKey => "facilities.swimming_pool";
}
