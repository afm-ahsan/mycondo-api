using Mediator;
using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.UpdateFinancialAccount;

public sealed record UpdateFinancialAccountCommand(
    Guid FinancialAccountId,
    string Name,
    string? BankName,
    string? BranchName,
    string? AccountNumber,
    Guid? FundId,
    string? Notes) : IRequest<FinancialAccountDto>;
