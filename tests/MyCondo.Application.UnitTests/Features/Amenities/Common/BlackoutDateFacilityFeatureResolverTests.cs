using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.Common;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Facilities;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Amenities.Common;

/// <summary>ADR-033 Task 09A §12/§45 — <see cref="BlackoutDateFacilityFeatureResolver{TRequest}"/> unit
/// tests.</summary>
public class BlackoutDateFacilityFeatureResolverTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IBlackoutDateRepository _blackoutDates = Substitute.For<IBlackoutDateRepository>();
    private readonly IFacilityRepository _facilities = Substitute.For<IFacilityRepository>();

    private sealed record Request(Guid BlackoutDateId) : IHasBlackoutDateId;

    private BlackoutDateFacilityFeatureResolver<Request> CreateSut() => new(_blackoutDates, _facilities);

    [Fact]
    public async Task Resolves_Through_The_Facility_The_Blackout_Date_Belongs_To()
    {
        Guid blackoutDateId = Guid.NewGuid();
        Guid facilityId = Guid.NewGuid();
        _blackoutDates.GetFacilityIdAsync(TenantId, new BlackoutDateId(blackoutDateId), Arg.Any<CancellationToken>())
            .Returns(new FacilityId(facilityId));
        _facilities.GetFacilityTypeAsync(TenantId, new FacilityId(facilityId), Arg.Any<CancellationToken>())
            .Returns(FacilityType.SwimmingPool);

        string featureKey = await CreateSut().ResolveFeatureKeyAsync(new Request(blackoutDateId), TenantId, CancellationToken.None);

        featureKey.Should().Be("facilities.swimming_pool");
    }

    [Fact]
    public async Task Throws_NotFoundException_When_The_Blackout_Date_Does_Not_Exist_Or_Is_Not_Owned_By_The_Tenant()
    {
        Guid blackoutDateId = Guid.NewGuid();
        _blackoutDates.GetFacilityIdAsync(TenantId, new BlackoutDateId(blackoutDateId), Arg.Any<CancellationToken>())
            .Returns((FacilityId?)null);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(blackoutDateId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
