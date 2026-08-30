using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.UpdateTenantFeatureOverride;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.UpdateTenantFeatureOverride;

public class UpdateTenantFeatureOverrideCommandHandlerTests
{
    private static readonly DateTimeOffset EffectiveFrom = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantFeatureOverrideRepository _tenantFeatureOverrides = Substitute.For<ITenantFeatureOverrideRepository>();
    private readonly IFeatureDefinitionRepository _featureDefinitions = Substitute.For<IFeatureDefinitionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FeatureDefinition _feature = FeatureDefinition.Create(
        "facilities.swimming_pool", "Swimming Pool", null, null, "facilities", FeatureCatalogueStatus.Active, false, 1,
        FeatureEntitlementType.Boolean);

    private TenantFeatureOverride _override = null!;

    public UpdateTenantFeatureOverrideCommandHandlerTests()
    {
        _override = TenantFeatureOverride.Create(
            _tenantId, _feature, true, EffectiveFrom, null, "Pilot enterprise customer", Guid.NewGuid(), EffectiveFrom);

        _tenantFeatureOverrides.GetByIdAsync(_override.Id, Arg.Any<CancellationToken>()).Returns(_override);
        _featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_feature]);
        _tenantFeatureOverrides.HasOverlappingOverrideAsync(
                Arg.Any<Guid>(), Arg.Any<FeatureDefinitionId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset?>(),
                Arg.Any<TenantFeatureOverrideId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    private UpdateTenantFeatureOverrideCommandHandler CreateHandler() => new(_tenantFeatureOverrides, _featureDefinitions, _unitOfWork);

    private UpdateTenantFeatureOverrideCommand ValidCommand() => new(
        OrganizationId: _tenantId,
        OverrideId: _override.Id.Value,
        Enabled: false,
        EffectiveFrom: EffectiveFrom.AddDays(1),
        EffectiveUntil: EffectiveFrom.AddMonths(1),
        Reason: "Revised reason");

    [Fact]
    public async Task Updates_The_Override_In_Place()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _override.Enabled.Should().BeFalse();
        _override.EffectiveFrom.Should().Be(EffectiveFrom.AddDays(1));
        _override.EffectiveUntil.Should().Be(EffectiveFrom.AddMonths(1));
        _override.Reason.Should().Be("Revised reason");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Excludes_Self_From_The_Overlap_PreCheck()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tenantFeatureOverrides.Received(1).HasOverlappingOverrideAsync(
            _tenantId, _feature.Id, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset?>(),
            _override.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Override_Does_Not_Exist()
    {
        Guid missingId = Guid.NewGuid();
        _tenantFeatureOverrides.GetByIdAsync(new TenantFeatureOverrideId(missingId), Arg.Any<CancellationToken>())
            .Returns((TenantFeatureOverride?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { OverrideId = missingId }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_OrganizationId_Does_Not_Match_The_Overrides_Own_Tenant()
    {
        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { OrganizationId = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_The_New_Window_Overlaps_Another_Override()
    {
        _tenantFeatureOverrides.HasOverlappingOverrideAsync(
                Arg.Any<Guid>(), Arg.Any<FeatureDefinitionId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset?>(),
                Arg.Any<TenantFeatureOverrideId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
