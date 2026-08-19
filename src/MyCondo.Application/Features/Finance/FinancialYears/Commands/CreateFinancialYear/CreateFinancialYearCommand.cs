using Mediator;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;

namespace MyCondo.Application.Features.Finance.FinancialYears.Commands.CreateFinancialYear;

public sealed record CreateFinancialYearCommand(string Name, DateOnly StartDate, DateOnly EndDate) : IRequest<FinancialYearDto>;
