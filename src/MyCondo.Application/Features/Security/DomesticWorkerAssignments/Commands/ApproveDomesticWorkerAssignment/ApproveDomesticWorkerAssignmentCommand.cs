using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.ApproveDomesticWorkerAssignment;

public sealed record ApproveDomesticWorkerAssignmentCommand(Guid DomesticWorkerAssignmentId) : IRequest, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.domestic_workers";
}
