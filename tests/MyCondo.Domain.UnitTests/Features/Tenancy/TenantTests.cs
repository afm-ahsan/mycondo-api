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

    [Fact]
    public void Reactivate_Transitions_Suspended_To_Active()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        tenant.Activate(Now);
        tenant.Suspend(Now);

        tenant.Reactivate(Now.AddMinutes(1));

        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Theory]
    [InlineData(TenantStatus.PendingActivation)]
    [InlineData(TenantStatus.Active)]
    public void Reactivate_Throws_Unless_Suspended(TenantStatus status)
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        if (status == TenantStatus.Active)
        {
            tenant.Activate(Now);
        }

        Action act = () => tenant.Reactivate(Now);

        act.Should().Throw<InvalidTenantStatusTransitionException>();
    }

    [Fact]
    public void Reactivate_Never_Changes_The_Behavior_Of_Activate()
    {
        // Guards the deliberate design choice (see Tenant.Reactivate's doc comment): Activate() must
        // stay PendingActivation-only forever, so the untouched tenant-side /api/v1/tenants/{id}/activate
        // endpoint never silently gains a new Suspended->Active path.
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        tenant.Activate(Now);
        tenant.Suspend(Now);

        Action act = () => tenant.Activate(Now);

        act.Should().Throw<InvalidTenantStatusTransitionException>();
    }

    [Fact]
    public void UpdateDetails_Normalizes_Name_And_Uppercases_Code()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);

        tenant.UpdateDetails("  Akter Residence Park  ", "  arp  ", Now);

        tenant.Name.Should().Be("Akter Residence Park");
        tenant.Code.Should().Be("ARP");
    }

    [Fact]
    public void UpdateDetails_Allows_Clearing_Code()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        tenant.UpdateDetails("ARP", "ARP", Now);

        tenant.UpdateDetails("ARP", null, Now);

        tenant.Code.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_Never_Changes_Slug()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);

        tenant.UpdateDetails("Renamed Org", "NEW-CODE", Now);

        tenant.Slug.Should().Be("arp");
    }

    [Fact]
    public void SetPrimaryAdministrator_Records_The_Snapshot()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", Now);
        Guid userId = Guid.NewGuid();

        tenant.SetPrimaryAdministrator(userId, "Admin", "admin@mycondo.com");

        tenant.PrimaryAdministratorUserId.Should().Be(userId);
        tenant.PrimaryAdministratorFullName.Should().Be("Admin");
        tenant.PrimaryAdministratorEmail.Should().Be("admin@mycondo.com");
    }

    [Fact]
    public void Provision_With_PreGenerated_Id_Uses_That_Id()
    {
        TenantId id = TenantId.New();

        Tenant tenant = Tenant.Provision(id, "ARP", "arp", Now);

        tenant.Id.Should().Be(id);
    }
}
