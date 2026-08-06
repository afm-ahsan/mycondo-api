namespace MyCondo.Application.Features.Payments.DTOs;

public sealed record ResidentAccountDto(
    Guid ResidentAccountId,
    Guid FlatId,
    DateTimeOffset OpenedAtUtc,
    bool IsActive);
