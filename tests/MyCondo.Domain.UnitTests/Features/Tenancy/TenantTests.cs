using AwesomeAssertions;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Domain.Features.Tenancy.Events;
using MyCondo.Domain.Features.Tenancy.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Tenancy;

public class TenantTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Provision_Normalizes_Slug_And_Starts_PendingActivation()
    {
        Tenant tenant = Tenant.Provision("ARP Flat Owners", "  ARP-Flat-Owners  ", Now);

        tenant.Name.Should().Be("ARP Flat Owners");
        tenant.Slug.Should().Be("arp-flat-owners");
        tenant.Status.Should().Be(TenantStatus.PendingActivation);
    }

    [Fact]
    public void Provision_Raises_TenantProvisionedEvent()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);

        tenant.DomainEvents.Should().ContainSingle(e => e is TenantProvisionedEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Provision_Throws_When_Name_Is_Blank(string name)
    {
        Action act = () => Tenant.Provision(name, "arp", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_Transitions_PendingActivation_To_Active()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);

        tenant.Activate(Now.AddMinutes(1));

        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantActivatedEvent);
    }

    [Fact]
    public void Activate_Is_A_NoOp_When_Already_Active()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        tenant.Activate(Now);

        tenant.Activate(Now.AddMinutes(1));

        tenant.DomainEvents.Should().ContainSingle(e => e is TenantActivatedEvent);
    }

    [Fact]
    public void Activate_Throws_When_Suspended()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        tenant.Activate(Now);
        tenant.Suspend(Now);

        Action act = () => tenant.Activate(Now);

        act.Should().Throw<InvalidTenantStatusTransitionException>();
    }

    [Fact]
    public void Suspend_Transitions_Active_To_Suspended()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        tenant.Activate(Now);

        tenant.Suspend(Now.AddMinutes(1));

        tenant.Status.Should().Be(TenantStatus.Suspended);
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantSuspendedEvent);
    }

    [Fact]
    public void Suspend_Throws_When_PendingActivation()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);

        Action act = () => tenant.Suspend(Now);

        act.Should().Throw<InvalidTenantStatusTransitionException>();
    }
}
