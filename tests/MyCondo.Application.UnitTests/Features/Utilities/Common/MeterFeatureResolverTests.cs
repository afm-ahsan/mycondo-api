using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Common;

/// <summary>ADR-033 Task 09A §15/§31/§45 — <see cref="MeterFeatureResolver{TRequest}"/> unit tests,
/// including the mandatory proof that the same shared request type resolves electricity vs. gas
/// differently by record.</summary>
public class MeterFeatureResolverTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IMeterRepository _meters = Substitute.For<IMeterRepository>();

    private sealed record Request(Guid MeterId) : IHasMeterId;

    private MeterFeatureResolver<Request> CreateSut() => new(_meters);

    [Fact]
    public async Task Same_Request_Type_Resolves_Electricity_And_Gas_Differently_By_Record()
    {
        Guid electricityMeterId = Guid.NewGuid();
        Guid gasMeterId = Guid.NewGuid();
        _meters.GetUtilityTypeAsync(TenantId, new MeterId(electricityMeterId), Arg.Any<CancellationToken>())
            .Returns(UtilityType.Electricity);
        _meters.GetUtilityTypeAsync(TenantId, new MeterId(gasMeterId), Arg.Any<CancellationToken>())
            .Returns(UtilityType.Gas);

        MeterFeatureResolver<Request> sut = CreateSut();

        string electricityKey = await sut.ResolveFeatureKeyAsync(new Request(electricityMeterId), TenantId, CancellationToken.None);
        string gasKey = await sut.ResolveFeatureKeyAsync(new Request(gasMeterId), TenantId, CancellationToken.None);

        electricityKey.Should().Be("utilities.electricity");
        gasKey.Should().Be("utilities.gas");
    }

    [Fact]
    public async Task Throws_NotFoundException_When_The_Meter_Does_Not_Exist_Or_Is_Not_Owned_By_The_Tenant()
    {
        Guid meterId = Guid.NewGuid();
        _meters.GetUtilityTypeAsync(TenantId, new MeterId(meterId), Arg.Any<CancellationToken>())
            .Returns((UtilityType?)null);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(meterId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_InvalidOperationException_For_An_Unmapped_UtilityType()
    {
        Guid meterId = Guid.NewGuid();
        _meters.GetUtilityTypeAsync(TenantId, new MeterId(meterId), Arg.Any<CancellationToken>())
            .Returns((UtilityType)999);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(meterId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
