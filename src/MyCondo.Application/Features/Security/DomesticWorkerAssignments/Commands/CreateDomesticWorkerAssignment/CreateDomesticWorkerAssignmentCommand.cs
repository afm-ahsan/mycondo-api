using Mediator;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.DTOs;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.CreateDomesticWorkerAssignment;

public sealed record CreateDomesticWorkerAssignmentCommand(
    Guid DomesticWorkerProfileId,
    Guid FlatId,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string? AllowedDays,
    TimeOnly? AllowedStartTime,
    TimeOnly? AllowedEndTime
) : IRequest<DomesticWorkerAssignmentDto>;
