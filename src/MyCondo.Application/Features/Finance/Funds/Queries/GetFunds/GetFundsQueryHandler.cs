using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Funds.DTOs;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Application.Features.Finance.Funds.Queries.GetFunds;

public sealed class GetFundsQueryHandler(
    IFundRepository funds,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFundsQuery, List<FundDto>>
{
    public async ValueTask<List<FundDto>> Handle(GetFundsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<Fund> allFunds = await funds.GetAllForTenantAsync(tenantId, cancellationToken);

        return allFunds.Select(f => new FundDto(f.Id.Value, f.Code, f.Name, f.Description, f.IsActive)).ToList();
    }
}
