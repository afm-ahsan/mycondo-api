namespace MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;

public sealed record AccountingPeriodDto(
    Guid AccountingPeriodId, Guid FinancialYearId, string Name, DateOnly StartDate, DateOnly EndDate, string Status);
