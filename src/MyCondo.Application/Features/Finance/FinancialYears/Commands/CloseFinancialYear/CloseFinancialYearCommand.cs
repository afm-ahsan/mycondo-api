using Mediator;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;

namespace MyCondo.Application.Features.Finance.FinancialYears.Commands.CloseFinancialYear;

public sealed record CloseFinancialYearCommand(Guid FinancialYearId) : IRequest<FinancialYearDto>;
