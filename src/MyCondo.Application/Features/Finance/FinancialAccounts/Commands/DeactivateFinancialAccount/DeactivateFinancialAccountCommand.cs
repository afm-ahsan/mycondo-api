using Mediator;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.DeactivateFinancialAccount;

public sealed record DeactivateFinancialAccountCommand(Guid FinancialAccountId) : IRequest;
