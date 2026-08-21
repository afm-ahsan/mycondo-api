using Mediator;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Commands.CreateAccountingPeriod;

public sealed record CreateAccountingPeriodCommand(
    Guid FinancialYearId, string Name, DateOnly StartDate, DateOnly EndDate
) : IRequest<AccountingPeriodDto>;
