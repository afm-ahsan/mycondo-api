using Mediator;
using MyCondo.Application.Features.Residents.DTOs;

namespace MyCondo.Application.Features.Residents.Commands.CreateResident;

public sealed record CreateResidentCommand(
    Guid FlatId,
    string FullName,
    string? Phone,
    string? Email,
    string ResidentType
) : IRequest<ResidentDto>;
