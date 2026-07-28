using MyCondo.Application.Common.Abstractions;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// A settable ITenantContextAccessor for tests — flip CurrentTenantId directly instead of going
/// through an HTTP request/JWT to establish "the current tenant" the way the real
/// MyCondo.Api.Authentication.TenantContextAccessor does.
/// </summary>
public sealed class TestTenantContextAccessor : ITenantContextAccessor
{
    public Guid? CurrentTenantId { get; set; }
}
