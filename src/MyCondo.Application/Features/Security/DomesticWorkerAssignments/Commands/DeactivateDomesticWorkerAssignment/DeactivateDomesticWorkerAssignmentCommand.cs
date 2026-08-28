using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.DeactivateDomesticWorkerAssignment;

public sealed record DeactivateDomesticWorkerAssignmentCommand(Guid DomesticWorkerAssignmentId) : IRequest, IRequiresFeature
{
    public string FeatureKey => "security.domestic_workers";
}
