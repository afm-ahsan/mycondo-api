using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.InstallMeter;

public sealed record InstallMeterCommand(Guid BuildingId, string UtilityType, string MeterNumber) : IRequest<MeterDto>;
