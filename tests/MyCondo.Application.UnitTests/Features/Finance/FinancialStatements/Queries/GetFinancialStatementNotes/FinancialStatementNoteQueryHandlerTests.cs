using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetFinancialPositionNotes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureNotes;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Queries.GetFinancialStatementNotes;

/// <summary>Both Notes query handlers: tenant default-deny, statement-aligned date semantics, fund
/// pass-through, and note-key filtering. The schedules themselves are covered by
/// <c>FinancialStatementNoteServiceTests</c>.</summary>
public class FinancialStatementNoteQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 6, 30, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly AsOf = new(2026, 6, 30);
    private static readonly DateOnly PeriodStart = new(2026, 6, 1);

    private readonly IFinancialStatementNoteService _noteService = Substitute.For<IFinancialStatementNoteService>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public FinancialStatementNoteQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(UserId);
        _clock.UtcNow.Returns(Now);

        _noteService.GetAsOfNotesAsync(
                Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(),
                Arg.Any<IReadOnlyCollection<FinancialStatementNoteKey>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _noteService.GetPeriodNotesAsync(
                Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(),
                Arg.Any<IReadOnlyCollection<FinancialStatementNoteKey>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private GetFinancialPositionNotesQueryHandler PositionHandler() => new(_noteService, _currentUser, _clock);

    private GetIncomeExpenditureNotesQueryHandler PeriodHandler() => new(_noteService, _currentUser, _clock);

    [Fact]
    public async Task Position_Notes_Require_A_Tenant_Context()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await PositionHandler()
            .Handle(new GetFinancialPositionNotesQuery(AsOf, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _noteService.DidNotReceive().GetAsOfNotesAsync(
            Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(),
            Arg.Any<IReadOnlyCollection<FinancialStatementNoteKey>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Income_Expenditure_Notes_Require_A_Tenant_Context()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await PeriodHandler()
            .Handle(new GetIncomeExpenditureNotesQuery(PeriodStart, AsOf, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Position_Notes_Default_To_Today_When_No_As_Of_Date_Is_Supplied()
    {
        await PositionHandler().Handle(new GetFinancialPositionNotesQuery(null, null, null), CancellationToken.None);

        await _noteService.Received().GetAsOfNotesAsync(
            TenantId, DateOnly.FromDateTime(Now.UtcDateTime), null,
            Arg.Any<IReadOnlyCollection<FinancialStatementNoteKey>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Position_Notes_Pass_The_Requested_Fund_And_Note_Selection_Through()
    {
        Guid fundId = Guid.NewGuid();

        await PositionHandler().Handle(
            new GetFinancialPositionNotesQuery(AsOf, fundId, [FinancialStatementNoteKey.CashAndBank]),
            CancellationToken.None);

        await _noteService.Received().GetAsOfNotesAsync(
            TenantId, AsOf, fundId,
            Arg.Is<IReadOnlyCollection<FinancialStatementNoteKey>>(
                k => k.Count == 1 && k.Contains(FinancialStatementNoteKey.CashAndBank)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Position_Notes_Metadata_Describes_The_As_Of_Scope()
    {
        FinancialStatementNotesDto dto = await PositionHandler()
            .Handle(new GetFinancialPositionNotesQuery(AsOf, null, null), CancellationToken.None);

        dto.Metadata.AsOfDate.Should().Be(AsOf);
        dto.Metadata.FromDate.Should().BeNull();
        dto.Metadata.ScopeSummary.Should().Be("Tenant (all funds)");
        dto.Metadata.GeneratedByUserId.Should().Be(UserId);
        dto.Metadata.PostedOnly.Should().BeTrue();
    }

    [Fact]
    public async Task Income_Expenditure_Notes_Metadata_Describes_The_Period_And_Fund_Scope()
    {
        Guid fundId = Guid.NewGuid();

        FinancialStatementNotesDto dto = await PeriodHandler()
            .Handle(new GetIncomeExpenditureNotesQuery(PeriodStart, AsOf, fundId, null), CancellationToken.None);

        dto.Metadata.FromDate.Should().Be(PeriodStart);
        dto.Metadata.ToDate.Should().Be(AsOf);
        dto.Metadata.AsOfDate.Should().BeNull();
        dto.Metadata.ScopeSummary.Should().Be("Single fund");
        dto.FundId.Should().Be(fundId);
    }

    [Fact]
    public async Task Notes_Are_Returned_Exactly_As_The_Service_Produced_Them()
    {
        FinancialStatementNote note = new(
            FinancialStatementNoteKey.CashAndBank, "Cash & Bank Balances", FinancialStatementNoteScope.AsOfDate,
            FinancialStatementGroup.CashAndBank, null, AsOf, null,
            GlBalance: 100_000m, ScheduleBalance: 90_000m, Difference: 10_000m, IsReconciled: false,
            UnattributedAmount: 10_000m, TotalRowCount: 1, IsDetailTruncated: false, Warnings: []);

        _noteService.GetAsOfNotesAsync(
                TenantId, AsOf, null, Arg.Any<IReadOnlyCollection<FinancialStatementNoteKey>>(),
                Arg.Any<CancellationToken>())
            .Returns([note]);

        FinancialStatementNotesDto dto = await PositionHandler()
            .Handle(new GetFinancialPositionNotesQuery(AsOf, null, null), CancellationToken.None);

        dto.Notes.Should().ContainSingle();
        dto.Notes[0].Difference.Should().Be(10_000m, "the handler must never smooth over a reconciliation gap");
        dto.Notes[0].IsReconciled.Should().BeFalse();
    }
}
