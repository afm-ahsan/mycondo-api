using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;

public class GetResidentFinancialStatementReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly BuildingId BuildingId = new(Guid.NewGuid());

    private readonly ILedgerEntryRepository _ledgerEntries = Substitute.For<ILedgerEntryRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IFlatAccessAuthorizer _flatAccessAuthorizer = Substitute.For<IFlatAccessAuthorizer>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetResidentFinancialStatementReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(UserId);
        _clock.UtcNow.Returns(Now);
    }

    private GetResidentFinancialStatementReportQueryHandler CreateHandler() =>
        new(_ledgerEntries, _flats, _flatAccessAuthorizer, _currentUser, _clock);

    private static Flat CreateFlat(FlatId? id = null)
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "A-101", 1, FlatType.Residential, Now);
        return flat;
    }

    private static LedgerEntryWithReference CreateReceivableEntry(FlatId flatId, LedgerDirection direction, decimal amount, DateOnly businessDate)
    {
        (_, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            TenantId, businessDate, "Test entry", "Invoice", Guid.NewGuid(),
            [
                new LedgerLine(LedgerAccountType.ResidentReceivable, flatId, direction, amount, "Test entry"),
                new LedgerLine(LedgerAccountType.AssociationRevenue, null,
                    direction == LedgerDirection.Debit ? LedgerDirection.Credit : LedgerDirection.Debit, amount, "Test entry"),
            ],
            Now);

        LedgerEntry receivableEntry = entries.Single(e => e.AccountType == LedgerAccountType.ResidentReceivable);
        return new LedgerEntryWithReference(receivableEntry, "Invoice", Guid.NewGuid());
    }

    [Fact]
    public async Task Running_Balance_Accumulates_Debits_Up_Credits_Down()
    {
        Flat flat = CreateFlat();
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _currentUser.HasPermission("finance.report.view").Returns(true);

        LedgerEntryWithReference charge = CreateReceivableEntry(flat.Id, LedgerDirection.Debit, 5_000m, new DateOnly(2026, 8, 5));
        LedgerEntryWithReference payment = CreateReceivableEntry(flat.Id, LedgerDirection.Credit, 3_000m, new DateOnly(2026, 8, 10));

        _ledgerEntries.SearchForFlatChronologicalAsync(TenantId, flat.Id, null, null, 1, 50, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LedgerEntryWithReference>([charge, payment], 1, 50, 2));

        ResidentFinancialStatementReportDto result = await CreateHandler().Handle(
            new GetResidentFinancialStatementReportQuery(flat.Id.Value, null, null, 1, 50), CancellationToken.None);

        result.Lines.Should().HaveCount(2);
        result.Lines[0].RunningBalance.Should().Be(5_000m);
        result.Lines[1].RunningBalance.Should().Be(2_000m);
        result.ClosingBalance.Should().Be(2_000m);
    }

    [Fact]
    public async Task Self_Service_Caller_Requesting_Own_Flat_Is_Allowed()
    {
        Flat flat = CreateFlat();
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _currentUser.HasPermission("finance.report.view").Returns(false);
        _currentUser.HasPermissionForBuilding("finance.report.statement.own.view", BuildingId.Value).Returns(true);
        _flatAccessAuthorizer.GetActiveRelationshipsAsync(TenantId, UserId, Arg.Any<CancellationToken>())
            .Returns([new FlatRelationship(flat.Id.Value, BuildingId.Value, FlatRelationshipKind.Ownership, null, null)]);

        _ledgerEntries.SearchForFlatChronologicalAsync(TenantId, flat.Id, null, null, 1, 50, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LedgerEntryWithReference>([], 1, 50, 0));

        ResidentFinancialStatementReportDto result = await CreateHandler().Handle(
            new GetResidentFinancialStatementReportQuery(flat.Id.Value, null, null, 1, 50), CancellationToken.None);

        result.FlatId.Should().Be(flat.Id.Value);
    }

    [Fact]
    public async Task Self_Service_Caller_Requesting_A_Different_Flat_Is_Rejected()
    {
        Flat ownFlat = CreateFlat();
        Flat otherFlat = CreateFlat();
        _flats.GetByIdAsync(otherFlat.Id, Arg.Any<CancellationToken>()).Returns(otherFlat);
        _currentUser.HasPermission("finance.report.view").Returns(false);
        _currentUser.HasPermissionForBuilding("finance.report.statement.own.view", Arg.Any<Guid?>()).Returns(true);
        _flatAccessAuthorizer.GetActiveRelationshipsAsync(TenantId, UserId, Arg.Any<CancellationToken>())
            .Returns([new FlatRelationship(ownFlat.Id.Value, BuildingId.Value, FlatRelationshipKind.Ownership, null, null)]);

        Func<Task> act = () => CreateHandler().Handle(
            new GetResidentFinancialStatementReportQuery(otherFlat.Id.Value, null, null, 1, 50), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Caller_With_No_Relevant_Permission_At_All_Is_Rejected()
    {
        Flat flat = CreateFlat();
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _currentUser.HasPermission("finance.report.view").Returns(false);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(false);
        _flatAccessAuthorizer.GetActiveRelationshipsAsync(TenantId, UserId, Arg.Any<CancellationToken>()).Returns([]);

        Func<Task> act = () => CreateHandler().Handle(
            new GetResidentFinancialStatementReportQuery(flat.Id.Value, null, null, 1, 50), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetResidentFinancialStatementReportQuery(Guid.NewGuid(), null, null, 1, 50), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
