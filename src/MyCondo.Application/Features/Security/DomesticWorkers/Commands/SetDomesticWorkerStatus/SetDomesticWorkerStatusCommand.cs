using Mediator;
using MyCondo.Application.Features.Security.DomesticWorkers.DTOs;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Commands.SetDomesticWorkerStatus;

/// <summary>Status must be one of Active/Suspended/Blocked. Reason is required unless Status is Active.</summary>
public sealed record SetDomesticWorkerStatusCommand(
    Guid DomesticWorkerProfileId,
    string Status,
    string? Reason
) : IRequest<DomesticWorkerProfileDto>;
