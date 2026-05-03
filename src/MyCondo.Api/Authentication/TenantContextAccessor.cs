using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authentication;

public sealed class TenantContextAccessor(ICurrentUserProvider currentUser) : ITenantContextAccessor
{
    public Guid? CurrentTenantId => currentUser.TenantId;
}
