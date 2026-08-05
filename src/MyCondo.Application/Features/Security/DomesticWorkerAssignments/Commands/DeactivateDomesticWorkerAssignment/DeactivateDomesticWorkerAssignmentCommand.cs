using Mediator;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.DeactivateDomesticWorkerAssignment;

public sealed record DeactivateDomesticWorkerAssignmentCommand(Guid DomesticWorkerAssignmentId) : IRequest;
