using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Common;

/// <summary>ADR-033 Task 09A §12/§45 — <see cref="FacilityFeatureResolver{TRequest}"/> unit tests.</summary>
public class FacilityFeatureResolverTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IFacilityRepository _facilities = Substitute.For<IFacilityRepository>();

    private sealed record Request(Guid FacilityId) : IHasFacilityId;

    private FacilityFeatureResolver<Request> CreateSut() => new(_facilities);

    [Fact]
    public async Task Resolves_Community_Hall_To_Its_Catalogue_Key()
    {
        Guid facilityId = Guid.NewGuid();
        _facilities.GetFacilityTypeAsync(TenantId, new FacilityId(facilityId), Arg.Any<CancellationToken>())
            .Returns(FacilityType.CommunityHall);

        string featureKey = await CreateSut().ResolveFeatureKeyAsync(new Request(facilityId), TenantId, CancellationToken.None);

        featureKey.Should().Be("facilities.community_hall");
    }

    [Fact]
    public async Task Resolves_Swimming_Pool_To_Its_Catalogue_Key()
    {
        Guid facilityId = Guid.NewGuid();
        _facilities.GetFacilityTypeAsync(TenantId, new FacilityId(facilityId), Arg.Any<CancellationToken>())
            .Returns(FacilityType.SwimmingPool);

        string featureKey = await CreateSut().ResolveFeatureKeyAsync(new Request(facilityId), TenantId, CancellationToken.None);

        featureKey.Should().Be("facilities.swimming_pool");
    }

    [Fact]
    public async Task Throws_NotFoundException_When_The_Facility_Does_Not_Exist_Or_Is_Not_Owned_By_The_Tenant()
    {
        Guid facilityId = Guid.NewGuid();
        _facilities.GetFacilityTypeAsync(TenantId, new FacilityId(facilityId), Arg.Any<CancellationToken>())
            .Returns((FacilityType?)null);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(facilityId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_InvalidOperationException_For_An_Unmapped_FacilityType()
    {
        Guid facilityId = Guid.NewGuid();
        _facilities.GetFacilityTypeAsync(TenantId, new FacilityId(facilityId), Arg.Any<CancellationToken>())
            .Returns((FacilityType)999);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(facilityId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
