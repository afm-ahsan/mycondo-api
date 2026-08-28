using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Readings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Common;

/// <summary>ADR-033 Task 09A §16/§31/§45 — <see cref="ReadingFeatureResolver{TRequest}"/> unit tests.</summary>
public class ReadingFeatureResolverTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IReadingRepository _readings = Substitute.For<IReadingRepository>();

    private sealed record Request(Guid ReadingId) : IHasReadingId;

    private ReadingFeatureResolver<Request> CreateSut() => new(_readings);

    [Fact]
    public async Task Same_Request_Type_Resolves_Electricity_And_Gas_Differently_By_Record()
    {
        Guid electricityReadingId = Guid.NewGuid();
        Guid gasReadingId = Guid.NewGuid();
        _readings.GetUtilityTypeAsync(TenantId, new ReadingId(electricityReadingId), Arg.Any<CancellationToken>())
            .Returns(UtilityType.Electricity);
        _readings.GetUtilityTypeAsync(TenantId, new ReadingId(gasReadingId), Arg.Any<CancellationToken>())
            .Returns(UtilityType.Gas);

        ReadingFeatureResolver<Request> sut = CreateSut();

        string electricityKey = await sut.ResolveFeatureKeyAsync(new Request(electricityReadingId), TenantId, CancellationToken.None);
        string gasKey = await sut.ResolveFeatureKeyAsync(new Request(gasReadingId), TenantId, CancellationToken.None);

        electricityKey.Should().Be("utilities.electricity");
        gasKey.Should().Be("utilities.gas");
    }

    [Fact]
    public async Task Throws_NotFoundException_When_The_Reading_Does_Not_Exist_Or_Is_Not_Owned_By_The_Tenant()
    {
        Guid readingId = Guid.NewGuid();
        _readings.GetUtilityTypeAsync(TenantId, new ReadingId(readingId), Arg.Any<CancellationToken>())
            .Returns((UtilityType?)null);

        Func<Task> act = () => CreateSut().ResolveFeatureKeyAsync(new Request(readingId), TenantId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
