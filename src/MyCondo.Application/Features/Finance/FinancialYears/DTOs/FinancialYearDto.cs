namespace MyCondo.Application.Features.Finance.FinancialYears.DTOs;

public sealed record FinancialYearDto(Guid FinancialYearId, string Name, DateOnly StartDate, DateOnly EndDate, string Status);
