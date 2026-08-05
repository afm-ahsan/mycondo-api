using Mediator;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.ApproveServiceProviderAssignment;

public sealed record ApproveServiceProviderAssignmentCommand(Guid ServiceProviderAssignmentId) : IRequest;
