using Mediator;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.ApproveDomesticWorkerAssignment;

public sealed record ApproveDomesticWorkerAssignmentCommand(Guid DomesticWorkerAssignmentId) : IRequest;
