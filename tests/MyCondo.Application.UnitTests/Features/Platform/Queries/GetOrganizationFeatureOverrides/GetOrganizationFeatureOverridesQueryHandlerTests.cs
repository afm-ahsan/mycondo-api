using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationFeatureOverrides;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetOrganizationFeatureOverrides;

public class GetOrganizationFeatureOverridesQueryHandlerTests
{
    private static readonly DateTimeOffset EffectiveFrom = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly ITenantFeatureOverrideRepository _tenantFeatureOverrides = Substitute.For<ITenantFeatureOverrideRepository>();
    private readonly IFeatureDefinitionRepository _featureDefinitions = Substitute.For<IFeatureDefinitionRepository>();

    private readonly Tenant _tenant = Tenant.Provision("Akter Residence Park", "arp", EffectiveFrom);
    private readonly FeatureDefinition _feature = FeatureDefinition.Create(
        "facilities.swimming_pool", "Swimming Pool", null, null, "facilities", FeatureCatalogueStatus.Active, false, 1,
        FeatureEntitlementType.Boolean);

    private GetOrganizationFeatureOverridesQueryHandler CreateHandler() => new(_tenants, _tenantFeatureOverrides, _featureDefinitions);

    [Fact]
    public async Task Returns_Overrides_With_Resolved_Feature_Key_And_Name()
    {
        _tenants.GetByIdAsync(_tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(_tenant);
        TenantFeatureOverride @override = TenantFeatureOverride.Create(
            _tenant.Id.Value, _feature, true, EffectiveFrom, null, "Pilot enterprise customer", Guid.NewGuid(), EffectiveFrom);
        _tenantFeatureOverrides.GetForTenantAsync(_tenant.Id.Value, Arg.Any<CancellationToken>()).Returns([@override]);
        _featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_feature]);

        List<TenantFeatureOverrideDto> result = await CreateHandler().Handle(
            new GetOrganizationFeatureOverridesQuery(_tenant.Id.Value), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(@override.Id.Value);
        result[0].FeatureKey.Should().Be(_feature.Key);
        result[0].FeatureName.Should().Be(_feature.Name);
        result[0].Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetOrganizationFeatureOverridesQuery(organizationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
