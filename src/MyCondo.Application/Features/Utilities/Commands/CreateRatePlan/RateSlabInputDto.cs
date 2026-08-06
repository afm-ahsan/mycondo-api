namespace MyCondo.Application.Features.Utilities.Commands.CreateRatePlan;

public sealed record RateSlabInputDto(int SlabOrder, decimal FromUnits, decimal? ToUnits, decimal RatePerUnit);
