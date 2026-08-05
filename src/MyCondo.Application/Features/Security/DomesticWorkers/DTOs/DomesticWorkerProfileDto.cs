namespace MyCondo.Application.Features.Security.DomesticWorkers.DTOs;

public sealed record DomesticWorkerProfileDto(
    Guid DomesticWorkerProfileId,
    string FullName,
    string Phone,
    string WorkerType,
    string? IdentityDocumentType,
    string? IdentityDocumentNumberMasked,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string VerificationStatus,
    string Status,
    string? StatusReason);
