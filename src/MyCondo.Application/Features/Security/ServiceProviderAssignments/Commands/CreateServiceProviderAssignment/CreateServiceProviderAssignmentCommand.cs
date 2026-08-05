using Mediator;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.CreateServiceProviderAssignment;

public sealed record CreateServiceProviderAssignmentCommand(
    Guid ServiceProviderProfileId,
    Guid FlatId,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string? AllowedDays,
    TimeOnly? AllowedStartTime,
    TimeOnly? AllowedEndTime
) : IRequest<ServiceProviderAssignmentDto>;
