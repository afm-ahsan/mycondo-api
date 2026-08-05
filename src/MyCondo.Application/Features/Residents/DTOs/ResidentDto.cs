namespace MyCondo.Application.Features.Residents.DTOs;

public sealed record ResidentDto(
    Guid ResidentId,
    Guid FlatId,
    string FullName,
    string? Phone,
    string? Email,
    string ResidentType);
