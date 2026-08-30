using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Common;

/// <summary>ADR-033 Task 09A §17/§31/§45 — <see cref="RatePlanFeatureResolver{TRequest}"/> unit tests.</summary>
public class RatePlanFeatureResolverTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IRatePlanRepository _ratePlans = Substitute.For<IRatePlanRepository>();

    private sealed record Request(Guid RatePlanId) : IHasRatePlanId;

    private RatePlanFeatureResolver<Request> CreateSut() => new(_ratePlans);

    [Fact]
    public async Task Same_Request_Type_Resolves_Electricity_And_Gas_Differently_By_Record()
    {
        Guid electricityPlanId = Guid.NewGuid();
        Guid gasPlanId = Guid.NewGuid();
        _ratePlans.GetUtilityTypeAsync(TenantId, new RatePlanId(electricityPlanId), Arg.Any<CancellationToken>())
            .Returns(UtilityType.Electricity);
        _ratePlans.GetUtilityTypeAsync(TenantId, new RatePlanId(gasPlanId), Arg.Any<CancellationToken>())
            .Returns(UtilityType.Gas);

        RatePlanFeatureResolver<Request> sut = CreateSut();

        string electricityKey = await sut.ResolveFeatureKeyAsync(new Request(electricityPlanId), TenantId, CancellationToken.None);
        string gasKey = await sut.ResolveFeatureKeyAsync(new Request(gasPlanId), TenantId, CancellationToken.None);

        electricityKey.Should().Be("utilities.electricity");
        gasKey.Should().Be("utilities.gas");
    }

    [Fact]
    public async Task Throws_NotFoundException_When_The_RatePlan_Does_Not_Exist_Or_Is_Not_Owned_By_The_Tenant()
    {
        Guid ratePlanId = Guid.NewGuid();
        _ratePlans.GetUtilityTypeAsync(TenantId, new RatePlanId(ratePlanId), Arg.Any<CancellationToken>())
            .Returns((UtilityType?)null);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(ratePlanId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
