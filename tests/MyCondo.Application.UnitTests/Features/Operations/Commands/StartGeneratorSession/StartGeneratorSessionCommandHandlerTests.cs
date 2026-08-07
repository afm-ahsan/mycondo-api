using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.Commands.StartGeneratorSession;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorSessions;
using MyCondo.Domain.Features.Operations.GeneratorSessions.Exceptions;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.Generators.Exceptions;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Operations.Commands.StartGeneratorSession;

/// <summary>
/// Proves "only one open session per generator" enforcement. The row lock itself
/// (<see cref="IGeneratorRepository.LockForSessionStartCheckAsync"/>) is only meaningfully provable
/// against a real Postgres instance under concurrent load (MultiTenancyTests, not executable in this
/// environment, same caveat as CheckInPoolSessionCommandHandlerTests) — this proves the single-request
/// enforcement logic the lock protects, and that the lock is actually invoked before the check.
/// </summary>
public class StartGeneratorSessionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GeneratorId GeneratorId = GeneratorId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IGeneratorRepository _generators = Substitute.For<IGeneratorRepository>();
    private readonly IGeneratorSessionRepository _sessions = Substitute.For<IGeneratorSessionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public StartGeneratorSessionCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IUnitOfWorkTransaction>());
    }

    private StartGeneratorSessionCommandHandler CreateHandler() => new(
        _generators, _sessions, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<StartGeneratorSessionCommandHandler>>());

    private static Generator ActiveGenerator() =>
        Generator.Create(TenantId, BuildingId.New(), "Generator 1", null, null, null, Now);

    private static StartGeneratorSessionCommand ValidCommand() => new(GeneratorId.Value, 40m);

    [Fact]
    public async Task Start_Succeeds_When_No_Open_Session_Exists()
    {
        _generators.GetByIdAsync(GeneratorId, Arg.Any<CancellationToken>()).Returns(ActiveGenerator());
        _sessions.GetOpenForGeneratorAsync(TenantId, GeneratorId, Arg.Any<CancellationToken>())
            .Returns((GeneratorSession?)null);

        GeneratorSessionDto result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Status.Should().Be("Open");
        await _generators.Received(1).LockForSessionStartCheckAsync(GeneratorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_Throws_When_An_Open_Session_Already_Exists()
    {
        _generators.GetByIdAsync(GeneratorId, Arg.Any<CancellationToken>()).Returns(ActiveGenerator());
        GeneratorSession openSession = GeneratorSession.Start(TenantId, GeneratorId, Guid.NewGuid(), 30m, Now);
        _sessions.GetOpenForGeneratorAsync(TenantId, GeneratorId, Arg.Any<CancellationToken>()).Returns(openSession);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<GeneratorAlreadyHasOpenSessionException>();
        _sessions.DidNotReceive().Add(Arg.Any<GeneratorSession>());
    }

    [Fact]
    public async Task Start_Throws_When_Generator_Is_Inactive()
    {
        Generator generator = ActiveGenerator();
        generator.Deactivate();
        _generators.GetByIdAsync(GeneratorId, Arg.Any<CancellationToken>()).Returns(generator);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<GeneratorInactiveException>();
        await _sessions.DidNotReceive().GetOpenForGeneratorAsync(TenantId, GeneratorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_Throws_NotFound_When_Generator_Belongs_To_Another_Tenant()
    {
        Generator otherTenantGenerator = Generator.Create(Guid.NewGuid(), BuildingId.New(), "Generator 1", null, null, null, Now);
        _generators.GetByIdAsync(GeneratorId, Arg.Any<CancellationToken>()).Returns(otherTenantGenerator);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
