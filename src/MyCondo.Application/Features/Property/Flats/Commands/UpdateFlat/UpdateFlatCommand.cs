using Mediator;
using MyCondo.Application.Features.Property.Flats.DTOs;

namespace MyCondo.Application.Features.Property.Flats.Commands.UpdateFlat;

public sealed record UpdateFlatCommand(
    Guid FlatId,
    string FlatNumber,
    int? FloorNumber,
    string FlatType
) : IRequest<FlatDto>;
