namespace MyCondo.Application.Features.Security.Guests.DTOs;

public sealed record GuestProfileDto(
    Guid GuestProfileId,
    string FullName,
    string Phone,
    string? IdentityDocumentType,
    string? IdentityDocumentNumberMasked,
    bool IsBlocked,
    string? BlockReason);
