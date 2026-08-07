using Mediator;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.CreateOccupancyRegistration;

public sealed record CreateOccupancyRegistrationCommand(
    Guid FlatId,
    string OccupancyType,
    string PrimaryFullName,
    string? PrimaryPhone,
    string? PrimaryEmail,
    string? PrimaryNationalIdNumber,
    DateOnly? PrimaryDateOfBirth,
    string? PrimaryPermanentAddress,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    DateOnly? MoveInExpectedDate
) : IRequest<OccupancyRegistrationDto>;
