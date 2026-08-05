using Mediator;
using MyCondo.Application.Features.Property.Flats.DTOs;

namespace MyCondo.Application.Features.Property.Flats.Commands.CreateFlat;

public sealed record CreateFlatCommand(
    Guid BuildingId,
    string FlatNumber,
    int? FloorNumber,
    string FlatType
) : IRequest<FlatDto>;
