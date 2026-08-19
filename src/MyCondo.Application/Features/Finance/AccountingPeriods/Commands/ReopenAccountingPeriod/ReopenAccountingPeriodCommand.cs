using Mediator;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Commands.ReopenAccountingPeriod;

public sealed record ReopenAccountingPeriodCommand(Guid AccountingPeriodId) : IRequest<AccountingPeriodDto>;
