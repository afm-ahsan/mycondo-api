using Mediator;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Commands.SoftCloseAccountingPeriod;

public sealed record SoftCloseAccountingPeriodCommand(Guid AccountingPeriodId) : IRequest<AccountingPeriodDto>;
