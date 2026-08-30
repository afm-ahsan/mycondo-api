using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInDomesticWorker;

public sealed record CheckInDomesticWorkerCommand(
    Guid DomesticWorkerProfileId,
    Guid HostFlatId,
    Guid EntryGateId,
    string? Remarks,
    string? OverrideReason
) : IRequest<AccessSessionDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.domestic_workers";
}
