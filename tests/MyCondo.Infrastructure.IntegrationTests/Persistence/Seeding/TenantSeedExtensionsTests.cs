using AwesomeAssertions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence.Seeding.Extensions;
using NSubstitute;

namespace MyCondo.Infrastructure.IntegrationTests.Persistence.Seeding;

public class TenantSeedExtensionsTests
{
    private static IClock FixedClock(DateTimeOffset now)
    {
        IClock clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    [Fact]
    public async Task EnsureTenantAsync_Creates_Active_Tenant_When_Slug_Not_Found()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        tenants.GetBySlugAsync("arp", Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Tenant? added = null;
        tenants.When(t => t.Add(Arg.Any<Tenant>())).Do(call => added = call.Arg<Tenant>());

        Tenant result = await tenants.EnsureTenantAsync(
            "Akter Residence Park", "arp", FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        added.Should().NotBeNull();
        result.Should().BeSameAs(added);
        result.Name.Should().Be("Akter Residence Park");
        result.Slug.Should().Be("arp");
        result.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public async Task EnsureTenantAsync_Returns_Existing_Tenant_Without_Adding_A_New_One()
    {
        ITenantRepository tenants = Substitute.For<ITenantRepository>();
        Tenant existing = Tenant.Provision("Akter Residence Park", "arp", DateTimeOffset.UtcNow);
        tenants.GetBySlugAsync("arp", Arg.Any<CancellationToken>()).Returns(existing);

        Tenant result = await tenants.EnsureTenantAsync(
            "Akter Residence Park", "arp", FixedClock(DateTimeOffset.UtcNow), CancellationToken.None);

        result.Should().BeSameAs(existing);
        tenants.DidNotReceive().Add(Arg.Any<Tenant>());
    }
}
