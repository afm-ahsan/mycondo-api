using Mediator;
using MyCondo.Application.Features.Finance.ChartOfAccounts.DTOs;

namespace MyCondo.Application.Features.Finance.ChartOfAccounts.Queries.GetChartOfAccounts;

public sealed record GetChartOfAccountsQuery : IRequest<List<ChartOfAccountDto>>;
