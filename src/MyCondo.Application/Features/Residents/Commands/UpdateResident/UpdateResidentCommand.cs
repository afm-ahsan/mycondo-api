using Mediator;
using MyCondo.Application.Features.Residents.DTOs;

namespace MyCondo.Application.Features.Residents.Commands.UpdateResident;

public sealed record UpdateResidentCommand(
    Guid ResidentId,
    string FullName,
    string? Phone,
    string? Email
) : IRequest<ResidentDto>;
