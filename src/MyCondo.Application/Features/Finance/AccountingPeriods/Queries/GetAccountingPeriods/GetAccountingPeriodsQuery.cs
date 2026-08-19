using Mediator;
using MyCondo.Application.Features.Finance.AccountingPeriods.DTOs;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Queries.GetAccountingPeriods;

public sealed record GetAccountingPeriodsQuery(Guid FinancialYearId) : IRequest<List<AccountingPeriodDto>>;
