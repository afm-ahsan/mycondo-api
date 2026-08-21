using Mediator;
using MyCondo.Application.Features.Finance.ChartOfAccounts.DTOs;

namespace MyCondo.Application.Features.Finance.ChartOfAccounts.Commands.CreateChartOfAccount;

public sealed record CreateChartOfAccountCommand(
    string Code,
    string Name,
    string Category,
    string NormalBalance,
    Guid? ParentAccountId
) : IRequest<ChartOfAccountDto>;
