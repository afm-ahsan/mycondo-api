using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFlatFinancialStatement;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetFlatFinancialStatement;

public class GetFlatFinancialStatementQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);

    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly ILedgerEntryRepository _ledgerEntries = Substitute.For<ILedgerEntryRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetFlatFinancialStatementQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetFlatFinancialStatementQueryHandler CreateHandler() => new(_flats, _ledgerEntries, _currentUser, _clock);

    private static Flat CreateFlat(Guid tenantId) =>
        Flat.Create(tenantId, new BuildingId(Guid.NewGuid()), "A-101", 1, FlatType.Residential, Now);

    // Builds a real ResidentReceivable LedgerEntry via LedgerPosting.Create (the only public way to
    // construct one) so the handler's running-balance math operates on genuine domain objects.
    private static LedgerEntryWithReference CreateReceivableEntry(
        Guid tenantId, FlatId flatId, LedgerDirection direction, decimal amount, DateOnly businessDate, string referenceType)
    {
        LedgerLine[] lines = direction == LedgerDirection.Debit
            ?
            [
                new LedgerLine(LedgerAccountType.ResidentReceivable, flatId, LedgerDirection.Debit, amount, "Charge"),
                new LedgerLine(LedgerAccountType.ServiceChargeIncome, null, LedgerDirection.Credit, amount, "Charge"),
            ]
            :
            [
                new LedgerLine(LedgerAccountType.ResidentReceivable, flatId, LedgerDirection.Credit, amount, "Payment"),
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, amount, "Payment"),
            ];

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, businessDate, "Test posting", referenceType, Guid.NewGuid(), lines, Now);

        LedgerEntry receivableEntry = entries.Single(e => e.AccountType == LedgerAccountType.ResidentReceivable);
        return new LedgerEntryWithReference(receivableEntry, posting.ReferenceType, posting.ReferenceId);
    }

    [Fact]
    public async Task Closing_Balance_And_Running_Balance_Derive_From_Opening_Plus_Chronological_Activity()
    {
        Flat flat = CreateFlat(TenantId);
        FlatId flatId = flat.Id;
        _flats.GetByIdAsync(flatId, Arg.Any<CancellationToken>()).Returns(flat);

        DateOnly from = new(2026, 8, 1);
        DateOnly to = new(2026, 8, 31);

        _ledgerEntries.GetReceivableBalanceForFlatBeforeAsync(TenantId, flatId, from, Arg.Any<CancellationToken>())
            .Returns(1_000m);
        _ledgerEntries.GetReceivableActivityForFlatAsync(TenantId, flatId, from, to, Arg.Any<CancellationToken>())
            .Returns((TotalDebit: 500m, TotalCredit: 300m));

        LedgerEntryWithReference charge = CreateReceivableEntry(TenantId, flatId, LedgerDirection.Debit, 500m, from, "Invoice");
        LedgerEntryWithReference payment = CreateReceivableEntry(TenantId, flatId, LedgerDirection.Credit, 300m, from.AddDays(5), "Payment");

        _ledgerEntries.SearchForFlatChronologicalAsync(TenantId, flatId, from, to, 1, 50, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LedgerEntryWithReference>([charge, payment], 1, 50, 2));

        FlatFinancialStatementReportDto result = await CreateHandler().Handle(
            new GetFlatFinancialStatementQuery(flatId.Value, from, to, 1, 50), CancellationToken.None);

        result.OpeningBalance.Should().Be(1_000m);
        result.PeriodDebitTotal.Should().Be(500m);
        result.PeriodCreditTotal.Should().Be(300m);
        result.ClosingBalance.Should().Be(1_200m); // 1,000 + 500 - 300
        result.Lines.Should().HaveCount(2);
        result.Lines[0].RunningBalance.Should().Be(1_500m); // 1,000 + 500 debit
        result.Lines[1].RunningBalance.Should().Be(1_200m); // 1,500 - 300 credit
    }

    [Fact]
    public async Task Unknown_Or_Cross_Tenant_Flat_Throws_NotFound()
    {
        FlatId flatId = new(Guid.NewGuid());
        _flats.GetByIdAsync(flatId, Arg.Any<CancellationToken>()).Returns((Flat?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetFlatFinancialStatementQuery(flatId.Value, null, null, 1, 50), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetFlatFinancialStatementQuery(Guid.NewGuid(), null, null, 1, 50), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
