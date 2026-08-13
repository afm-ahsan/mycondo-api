namespace MyCondo.Application.Features.Property.Buildings.DTOs;

public sealed record BuildingDto(Guid BuildingId, string Name, string Code, string? Address, Guid? PrimaryPhotoAttachmentId);
