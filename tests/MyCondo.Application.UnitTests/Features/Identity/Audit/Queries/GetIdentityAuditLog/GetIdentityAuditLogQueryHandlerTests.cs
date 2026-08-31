using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Identity.Audit.DTOs;
using MyCondo.Application.Features.Identity.Audit.Queries.GetIdentityAuditLog;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Identity.Audit.Queries.GetIdentityAuditLog;

/// <summary>
/// Mirrors GetFinanceAuditLogQueryHandlerTests' coverage of the actor-name resolution fallback chain
/// for <see cref="IdentityAuditLogEntry"/> (mycondo-docs ADR-036), plus the tenant-scoping guard every
/// query handler enforces before touching the repository.
/// </summary>
public class GetIdentityAuditLogQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IIdentityAuditLogRepository _auditLog = Substitute.For<IIdentityAuditLogRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    private GetIdentityAuditLogQueryHandler CreateHandler() => new(_auditLog, _users, _currentUser);

    [Fact]
    public async Task Handle_Resolves_ActorUserId_To_The_Actor_Full_Name()
    {
        _currentUser.TenantId.Returns(TenantId);
        User actor = User.Register(TenantId, "ahsan@mycondo.test", "hash", "Ahsan Uddin", null, NowUtc);
        IdentityAuditLogEntry entry = IdentityAuditLogEntry.Record(
            TenantId, NowUtc, actor.Id.Value, "User.Deactivate", "User", Guid.NewGuid().ToString());

        _auditLog.GetRecentAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([actor]);

        List<IdentityAuditLogEntryDto> result = await CreateHandler().Handle(
            new GetIdentityAuditLogQuery(100), CancellationToken.None);

        result.Should().ContainSingle(e => e.ActorDisplayName == "Ahsan Uddin");
    }

    [Fact]
    public async Task Handle_Uses_System_When_ActorUserId_Is_Null()
    {
        _currentUser.TenantId.Returns(TenantId);
        IdentityAuditLogEntry entry = IdentityAuditLogEntry.Record(TenantId, NowUtc, null, "User.Create");

        _auditLog.GetRecentAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        List<IdentityAuditLogEntryDto> result = await CreateHandler().Handle(
            new GetIdentityAuditLogQuery(100), CancellationToken.None);

        result.Should().ContainSingle(e => e.ActorDisplayName == "System");
    }

    [Fact]
    public async Task Handle_Uses_Unknown_User_When_The_Actor_No_Longer_Exists()
    {
        _currentUser.TenantId.Returns(TenantId);
        Guid deletedActorId = Guid.NewGuid();
        IdentityAuditLogEntry entry = IdentityAuditLogEntry.Record(TenantId, NowUtc, deletedActorId, "User.Role.Revoke");

        _auditLog.GetRecentAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        List<IdentityAuditLogEntryDto> result = await CreateHandler().Handle(
            new GetIdentityAuditLogQuery(100), CancellationToken.None);

        result.Should().ContainSingle(e => e.ActorDisplayName == "Unknown user");
    }

    [Fact]
    public async Task Handle_Clamps_Take_To_The_500_Row_Maximum()
    {
        _currentUser.TenantId.Returns(TenantId);
        _auditLog.GetRecentAsync(TenantId, 500, Arg.Any<CancellationToken>()).Returns([]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>()).Returns([]);

        await CreateHandler().Handle(new GetIdentityAuditLogQuery(10_000), CancellationToken.None);

        await _auditLog.Received(1).GetRecentAsync(TenantId, 500, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Not_Authenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetIdentityAuditLogQuery(100), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
