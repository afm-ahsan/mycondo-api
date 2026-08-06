using Mediator;
using MyCondo.Application.Features.Property.Flats.DTOs;

namespace MyCondo.Application.Features.Property.Flats.Commands.UpdateFlatArea;

public sealed record UpdateFlatAreaCommand(Guid FlatId, decimal? AreaSqFt) : IRequest<FlatDto>;
