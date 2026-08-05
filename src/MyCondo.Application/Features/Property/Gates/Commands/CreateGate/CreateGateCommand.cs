using Mediator;
using MyCondo.Application.Features.Property.Gates.DTOs;

namespace MyCondo.Application.Features.Property.Gates.Commands.CreateGate;

public sealed record CreateGateCommand(Guid BuildingId, string Name) : IRequest<GateDto>;
