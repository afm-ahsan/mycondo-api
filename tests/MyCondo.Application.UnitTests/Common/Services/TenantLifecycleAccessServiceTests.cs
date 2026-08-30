using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>Application-layer tests for <see cref="TenantLifecycleAccessService"/> (ADR-032 Task 10 §8) —
/// verifies it is a thin, single-query state-retrieval wrapper around <see cref="ITenantRepository"/>.</summary>
public class TenantLifecycleAccessServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();

    private TenantLifecycleAccessService CreateSut() => new(_tenants);

    [Theory]
    [InlineData(TenantStatus.Active)]
    [InlineData(TenantStatus.Suspended)]
    [InlineData(TenantStatus.Closed)]
    [InlineData(TenantStatus.PendingActivation)]
    public async Task Returns_The_Tenants_Current_Status(TenantStatus status)
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        if (status != TenantStatus.PendingActivation)
        {
            tenant.Activate(Now);
        }
        if (status == TenantStatus.Suspended)
        {
            tenant.Suspend(Now);
        }
        if (status == TenantStatus.Closed)
        {
            tenant.Close(Now);
        }
        _tenants.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        TenantStatus result = await CreateSut().GetTenantStatusAsync(TenantId, CancellationToken.None);

        result.Should().Be(status);
    }

    [Fact]
    public async Task Throws_NotFound_When_The_Tenant_Does_Not_Exist()
    {
        _tenants.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = () => CreateSut().GetTenantStatusAsync(TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
