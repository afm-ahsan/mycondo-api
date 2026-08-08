namespace MyCondo.Application.Features.Utilities.DTOs;

public sealed record ConsumptionSummaryLineDto(string UtilityType, decimal TotalConsumptionUnits, int ReadingCount);
