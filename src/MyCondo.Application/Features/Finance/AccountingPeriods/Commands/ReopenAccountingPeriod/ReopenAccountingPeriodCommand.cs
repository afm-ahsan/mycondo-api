using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Commands.ReopenAccountingPeriod;

public sealed record ReopenAccountingPeriodCommand(Guid AccountingPeriodId) : IRequest<AccountingPeriodDto>, ILifecycleWriteOperation;
