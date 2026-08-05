using Mediator;
using MyCondo.Application.Features.Security.DomesticWorkers.DTOs;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Commands.RegisterDomesticWorker;

public sealed record RegisterDomesticWorkerCommand(
    string FullName,
    string Phone,
    string WorkerType,
    string? IdentityDocumentType,
    string? IdentityDocumentNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone
) : IRequest<DomesticWorkerProfileDto>;
