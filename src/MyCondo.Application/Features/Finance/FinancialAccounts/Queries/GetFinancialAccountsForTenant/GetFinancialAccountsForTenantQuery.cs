using Mediator;
using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Queries.GetFinancialAccountsForTenant;

public sealed record GetFinancialAccountsForTenantQuery : IRequest<List<FinancialAccountDto>>;
