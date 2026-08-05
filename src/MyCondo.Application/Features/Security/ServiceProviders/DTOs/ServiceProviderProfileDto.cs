namespace MyCondo.Application.Features.Security.ServiceProviders.DTOs;

public sealed record ServiceProviderProfileDto(
    Guid ServiceProviderProfileId,
    string FullName,
    string Phone,
    string ProviderType,
    string? ServiceDescription,
    string? IdentityDocumentType,
    string? IdentityDocumentNumberMasked,
    string VerificationStatus,
    string Status,
    string? StatusReason);
