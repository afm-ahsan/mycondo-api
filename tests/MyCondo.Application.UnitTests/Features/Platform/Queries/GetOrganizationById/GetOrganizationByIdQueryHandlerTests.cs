using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationById;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetOrganizationById;

public class GetOrganizationByIdQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly ITenantModuleRepository _tenantModules = Substitute.For<ITenantModuleRepository>();

    private GetOrganizationByIdQueryHandler CreateHandler() => new(_tenants, _tenantModules);

    [Fact]
    public async Task Returns_Full_Detail_Including_Administrator_And_Enabled_Modules()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        Guid adminId = Guid.NewGuid();
        tenant.SetPrimaryAdministrator(adminId, "Admin", "admin@mycondo.com");
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _tenantModules.GetEnabledForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns([TenantModule.Enable(tenant.Id.Value, "billing", NowUtc, null)]);

        OrganizationDetailDto result =
            await CreateHandler().Handle(new GetOrganizationByIdQuery(tenant.Id.Value), CancellationToken.None);

        result.Administrator.Should().NotBeNull();
        result.Administrator!.UserId.Should().Be(adminId);
        result.EnabledModuleKeys.Should().ContainSingle().Which.Should().Be("billing");
    }

    [Fact]
    public async Task Returns_Null_Administrator_When_None_Was_Ever_Set()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _tenantModules.GetEnabledForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns([]);

        OrganizationDetailDto result =
            await CreateHandler().Handle(new GetOrganizationByIdQuery(tenant.Id.Value), CancellationToken.None);

        result.Administrator.Should().BeNull();
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () =>
            await CreateHandler().Handle(new GetOrganizationByIdQuery(organizationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
