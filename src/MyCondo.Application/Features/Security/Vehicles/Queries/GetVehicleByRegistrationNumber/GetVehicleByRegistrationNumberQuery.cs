using Mediator;
using MyCondo.Application.Features.Security.Vehicles.DTOs;

namespace MyCondo.Application.Features.Security.Vehicles.Queries.GetVehicleByRegistrationNumber;

public sealed record GetVehicleByRegistrationNumberQuery(string RegistrationNumber) : IRequest<VehicleDto?>;
