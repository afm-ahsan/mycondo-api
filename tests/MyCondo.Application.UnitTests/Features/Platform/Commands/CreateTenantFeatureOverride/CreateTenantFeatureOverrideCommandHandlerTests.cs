using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.CreateTenantFeatureOverride;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides.Exceptions;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.CreateTenantFeatureOverride;

public class CreateTenantFeatureOverrideCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IFeatureDefinitionRepository _featureDefinitions = Substitute.For<IFeatureDefinitionRepository>();
    private readonly ITenantFeatureOverrideRepository _tenantFeatureOverrides = Substitute.For<ITenantFeatureOverrideRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly Tenant _tenant = Tenant.Provision("Akter Residence Park", "arp", NowUtc);
    private readonly FeatureDefinition _feature = FeatureDefinition.Create(
        "facilities.swimming_pool", "Swimming Pool", null, null, "facilities", FeatureCatalogueStatus.Active, false, 1,
        FeatureEntitlementType.Boolean);

    public CreateTenantFeatureOverrideCommandHandlerTests()
    {
        _tenants.GetByIdAsync(_tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(_tenant);
        _featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_feature]);
        _tenantFeatureOverrides.HasOverlappingOverrideAsync(
                Arg.Any<Guid>(), Arg.Any<FeatureDefinitionId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset?>(),
                Arg.Any<TenantFeatureOverrideId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _clock.UtcNow.Returns(NowUtc);
    }

    private CreateTenantFeatureOverrideCommandHandler CreateHandler() => new(
        _tenants, _featureDefinitions, _tenantFeatureOverrides, _unitOfWork, _clock,
        Substitute.For<ILogger<CreateTenantFeatureOverrideCommandHandler>>());

    private CreateTenantFeatureOverrideCommand ValidCommand() => new(
        OrganizationId: _tenant.Id.Value,
        FeatureKey: _feature.Key,
        Enabled: true,
        EffectiveFrom: NowUtc,
        EffectiveUntil: null,
        Reason: "Pilot enterprise customer",
        CreatedBy: Guid.NewGuid());

    [Fact]
    public async Task Creates_And_Persists_The_Override()
    {
        Guid overrideId = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        overrideId.Should().NotBeEmpty();
        _tenantFeatureOverrides.Received(1).Add(Arg.Is<TenantFeatureOverride>(o =>
            o.TenantId == _tenant.Id.Value && o.FeatureId == _feature.Id && o.Enabled));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { OrganizationId = organizationId }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_FeatureKey_Does_Not_Exist()
    {
        CreateTenantFeatureOverrideCommand command = ValidCommand() with { FeatureKey = "does.not.exist" };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_An_Overlapping_Override_Exists()
    {
        _tenantFeatureOverrides.HasOverlappingOverrideAsync(
                Arg.Any<Guid>(), Arg.Any<FeatureDefinitionId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset?>(),
                Arg.Any<TenantFeatureOverrideId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Feature_Is_Core()
    {
        FeatureDefinition coreFeature = FeatureDefinition.Create(
            "core.dashboard", "Dashboard", null, null, "core", FeatureCatalogueStatus.Active, isCore: true, 1,
            FeatureEntitlementType.Boolean);
        _featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([coreFeature]);

        CreateTenantFeatureOverrideCommand command = ValidCommand() with { FeatureKey = coreFeature.Key };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<CoreFeatureOverrideNotAllowedException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Feature_Is_Reserved()
    {
        FeatureDefinition reservedFeature = FeatureDefinition.Create(
            "legacy.retired", "Retired", null, null, "legacy", FeatureCatalogueStatus.Reserved, false, 1,
            FeatureEntitlementType.Boolean);
        _featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([reservedFeature]);

        CreateTenantFeatureOverrideCommand command = ValidCommand() with { FeatureKey = reservedFeature.Key };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ReservedFeatureOverrideNotAllowedException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
