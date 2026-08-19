using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.AccountMappings.DTOs;
using MyCondo.Domain.Features.Finance.AccountMappings;

namespace MyCondo.Application.Features.Finance.AccountMappings.Queries.GetAccountMappings;

public sealed class GetAccountMappingsQueryHandler(
    IAccountMappingRepository accountMappings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAccountMappingsQuery, List<AccountMappingDto>>
{
    public async ValueTask<List<AccountMappingDto>> Handle(GetAccountMappingsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<AccountMapping> mappings = await accountMappings.GetAllForTenantAsync(tenantId, cancellationToken);

        return mappings
            .Select(m => new AccountMappingDto(m.Id.Value, m.PostingRole, m.ChartOfAccountId.Value))
            .ToList();
    }
}
