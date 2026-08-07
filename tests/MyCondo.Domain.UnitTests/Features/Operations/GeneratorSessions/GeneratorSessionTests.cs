using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.GeneratorSessions;
using MyCondo.Domain.Features.Operations.GeneratorSessions.Exceptions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.UnitTests.Features.Operations.GeneratorSessions;

public class GeneratorSessionTests
{
    private static readonly DateTimeOffset StartAt = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GeneratorId GeneratorId = GeneratorId.New();

    [Fact]
    public void Start_Creates_Open_Session_With_No_Runtime()
    {
        GeneratorSession session = GeneratorSession.Start(TenantId, GeneratorId, Guid.NewGuid(), 40m, StartAt);

        session.Status.Should().Be(GeneratorSessionStatus.Open);
        session.StopAtUtc.Should().BeNull();
        session.RuntimeMinutes.Should().BeNull();
    }

    [Fact]
    public void Start_Throws_When_OpeningFuelLevel_Negative()
    {
        Action act = () => GeneratorSession.Start(TenantId, GeneratorId, Guid.NewGuid(), -1m, StartAt);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Stop_Computes_RuntimeMinutes_Server_Side()
    {
        GeneratorSession session = GeneratorSession.Start(TenantId, GeneratorId, Guid.NewGuid(), 40m, StartAt);

        session.Stop(25m, null, StartAt.AddMinutes(90));

        session.Status.Should().Be(GeneratorSessionStatus.Closed);
        session.RuntimeMinutes.Should().Be(90);
        session.ClosingFuelLevel.Should().Be(25m);
    }

    [Fact]
    public void Stop_Throws_When_Already_Closed()
    {
        GeneratorSession session = GeneratorSession.Start(TenantId, GeneratorId, Guid.NewGuid(), 40m, StartAt);
        session.Stop(25m, null, StartAt.AddMinutes(60));

        Action act = () => session.Stop(20m, null, StartAt.AddMinutes(90));

        act.Should().Throw<GeneratorSessionAlreadyClosedException>();
    }

    [Fact]
    public void Stop_Throws_When_Stop_Time_Precedes_Start_Time()
    {
        GeneratorSession session = GeneratorSession.Start(TenantId, GeneratorId, Guid.NewGuid(), 40m, StartAt);

        Action act = () => session.Stop(25m, null, StartAt.AddMinutes(-5));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
