using Mediator;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.ActivateFinancialAccount;

public sealed record ActivateFinancialAccountCommand(Guid FinancialAccountId) : IRequest;
