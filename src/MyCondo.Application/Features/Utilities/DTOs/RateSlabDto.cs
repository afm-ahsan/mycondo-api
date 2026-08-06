namespace MyCondo.Application.Features.Utilities.DTOs;

public sealed record RateSlabDto(int SlabOrder, decimal FromUnits, decimal? ToUnits, decimal RatePerUnit);
