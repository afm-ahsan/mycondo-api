using AwesomeAssertions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.ListOrganizations;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.ListOrganizations;

public class ListOrganizationsQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly ITenantModuleRepository _tenantModules = Substitute.For<ITenantModuleRepository>();

    private ListOrganizationsQueryHandler CreateHandler() => new(_tenants, _tenantModules);

    [Fact]
    public async Task Maps_Tenants_To_List_Items_With_Module_Counts_And_Administrator_Snapshot()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        tenant.Activate(NowUtc);
        tenant.UpdateDetails("Akter Residence Park", "ARP", NowUtc);
        tenant.SetPrimaryAdministrator(Guid.NewGuid(), "Admin", "admin@mycondo.com");

        _tenants.SearchAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Tenant>([tenant], 1, 20, 1));
        _tenantModules.GetEnabledCountsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(tenant.Id.Value)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [tenant.Id.Value] = 3 });

        PagedResult<OrganizationListItemDto> result =
            await CreateHandler().Handle(new ListOrganizationsQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
        OrganizationListItemDto item = result.Items[0];
        item.Name.Should().Be("Akter Residence Park");
        item.Code.Should().Be("ARP");
        item.PrimaryAdministratorEmail.Should().Be("admin@mycondo.com");
        item.EnabledModuleCount.Should().Be(3);
    }

    [Fact]
    public async Task Defaults_Module_Count_To_Zero_When_Tenant_Has_No_Enabled_Modules()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        _tenants.SearchAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Tenant>([tenant], 1, 20, 1));
        _tenantModules.GetEnabledCountsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        PagedResult<OrganizationListItemDto> result =
            await CreateHandler().Handle(new ListOrganizationsQuery(), CancellationToken.None);

        result.Items[0].EnabledModuleCount.Should().Be(0);
    }

    [Fact]
    public async Task Parses_The_Status_Filter_Before_Searching()
    {
        _tenants.SearchAsync(1, 20, null, TenantStatus.Suspended, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Tenant>([], 1, 20, 0));
        _tenantModules.GetEnabledCountsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        await CreateHandler().Handle(new ListOrganizationsQuery(Status: "Suspended"), CancellationToken.None);

        await _tenants.Received(1).SearchAsync(1, 20, null, TenantStatus.Suspended, Arg.Any<CancellationToken>());
    }
}
