using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutServiceProvider;

public sealed record CheckOutServiceProviderCommand(Guid AccessSessionId, Guid ExitGateId) : IRequest<AccessSessionDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.service_providers";
}
