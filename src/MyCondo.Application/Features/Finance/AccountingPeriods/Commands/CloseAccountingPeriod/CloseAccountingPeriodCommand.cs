using Mediator;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Commands.CloseAccountingPeriod;

public sealed record CloseAccountingPeriodCommand(Guid AccountingPeriodId) : IRequest<AccountingPeriodDto>;
