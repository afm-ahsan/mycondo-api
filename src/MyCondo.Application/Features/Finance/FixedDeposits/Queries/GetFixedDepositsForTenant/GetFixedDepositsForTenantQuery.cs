using Mediator;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Queries.GetFixedDepositsForTenant;

public sealed record GetFixedDepositsForTenantQuery(
    string? Status,
    Guid? FundId,
    Guid? FundingFinancialAccountId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<FixedDepositDto>>;
