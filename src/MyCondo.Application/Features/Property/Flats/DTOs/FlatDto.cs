namespace MyCondo.Application.Features.Property.Flats.DTOs;

public sealed record FlatDto(
    Guid FlatId,
    Guid BuildingId,
    string FlatNumber,
    int? FloorNumber,
    string FlatType);
