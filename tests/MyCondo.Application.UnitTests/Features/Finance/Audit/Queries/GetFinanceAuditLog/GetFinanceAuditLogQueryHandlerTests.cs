using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Audit.DTOs;
using MyCondo.Application.Features.Finance.Audit.Queries.GetFinanceAuditLog;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Audit.Queries.GetFinanceAuditLog;

/// <summary>
/// Regression coverage for the MVP-1 Finance Audit Log defect: the actor who performed a sensitive
/// governance action (period close, void, waiver, etc.) was silently dropped from every response —
/// not even shown as a raw GUID. Locks in the resolved-name/System/Unknown-user fallback chain,
/// mirroring GetCustodyHistoryForParcelQueryHandlerTests' coverage of the same pattern.
/// </summary>
public class GetFinanceAuditLogQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 18, 5, 56, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IFinanceAuditLogRepository _auditLog = Substitute.For<IFinanceAuditLogRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    private GetFinanceAuditLogQueryHandler CreateHandler() => new(_auditLog, _users, _currentUser);

    [Fact]
    public async Task Handle_Resolves_ActorUserId_To_The_Actor_Full_Name()
    {
        _currentUser.TenantId.Returns(TenantId);
        User actor = User.Register(TenantId, "ahsan@mycondo.test", "hash", "Ahsan Uddin", null, NowUtc);
        FinanceAuditLogEntry entry = FinanceAuditLogEntry.Record(
            TenantId, NowUtc, actor.Id.Value, "Invoice.Void", "Invoice", Guid.NewGuid().ToString());

        _auditLog.GetRecentAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([actor]);

        List<FinanceAuditLogEntryDto> result = await CreateHandler().Handle(
            new GetFinanceAuditLogQuery(100), CancellationToken.None);

        result.Should().ContainSingle(e => e.ActorDisplayName == "Ahsan Uddin");
    }

    [Fact]
    public async Task Handle_Uses_System_When_ActorUserId_Is_Null()
    {
        _currentUser.TenantId.Returns(TenantId);
        FinanceAuditLogEntry entry = FinanceAuditLogEntry.Record(
            TenantId, NowUtc, null, "AccountingPeriod.Close");

        _auditLog.GetRecentAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        List<FinanceAuditLogEntryDto> result = await CreateHandler().Handle(
            new GetFinanceAuditLogQuery(100), CancellationToken.None);

        result.Should().ContainSingle(e => e.ActorDisplayName == "System");
    }

    [Fact]
    public async Task Handle_Uses_Unknown_User_When_The_Actor_No_Longer_Exists()
    {
        _currentUser.TenantId.Returns(TenantId);
        Guid deletedActorId = Guid.NewGuid();
        FinanceAuditLogEntry entry = FinanceAuditLogEntry.Record(
            TenantId, NowUtc, deletedActorId, "Payment.Reverse");

        _auditLog.GetRecentAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([entry]);
        _users.GetByIdsAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        List<FinanceAuditLogEntryDto> result = await CreateHandler().Handle(
            new GetFinanceAuditLogQuery(100), CancellationToken.None);

        result.Should().ContainSingle(e => e.ActorDisplayName == "Unknown user");
    }
}
