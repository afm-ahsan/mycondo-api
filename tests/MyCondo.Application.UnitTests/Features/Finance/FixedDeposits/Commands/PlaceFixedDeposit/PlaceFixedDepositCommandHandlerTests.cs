using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FixedDeposits.Commands.PlaceFixedDeposit;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FixedDeposits.Commands.PlaceFixedDeposit;

public class PlaceFixedDepositCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IFixedDepositRepository _fixedDeposits = Substitute.For<IFixedDepositRepository>();
    private readonly IFinancialAccountRepository _financialAccounts = Substitute.For<IFinancialAccountRepository>();
    private readonly IFundRepository _funds = Substitute.For<IFundRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public PlaceFixedDepositCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
        StubFinancialPosting();
    }

    /// <summary>Makes the mocked <see cref="IFinancialPostingService"/> behave like the real one — same
    /// helper pattern as <c>VoidExpenseCommandHandlerTests</c>.</summary>
    private void StubFinancialPosting() =>
        _financialPosting.PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                FinancialPostingRequest request = callInfo.Arg<FinancialPostingRequest>();
                List<LedgerLine> lines = request.Lines
                    .Select(l => new LedgerLine(l.Role, l.FlatId, l.Direction, l.Amount, l.LineDescription ?? request.Description))
                    .ToList();
                (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
                    request.TenantId, request.BusinessDate, request.Description, request.PostingPurpose,
                    request.SourceId, lines, NowUtc);
                foreach (LedgerEntry entry in entries)
                {
                    entry.SetFinanceDimensions(ChartOfAccountId.New(), request.FundId, null);
                }
                return new FinancialPostingResult(posting, entries);
            });

    private PlaceFixedDepositCommandHandler CreateHandler() => new(
        _fixedDeposits, _financialAccounts, _funds, _financialPosting, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<PlaceFixedDepositCommandHandler>>());

    private FinancialAccount SetUpFundingAccount(bool isActive = true)
    {
        FinancialAccount account = FinancialAccount.Create(
            TenantId, "Main Bank", FinancialAccountType.Bank, "City Bank", null, "123", ChartOfAccountId.New(),
            fundId: null, notes: null);
        if (!isActive)
        {
            account.Deactivate();
        }

        _financialAccounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        return account;
    }

    private static PlaceFixedDepositCommand ValidCommand(Guid fundingAccountId) => new(
        "FD-001", "City Bank", "Gulshan", fundingAccountId, null, 500_000m, 7.5m, "Simple", "Monthly",
        new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), null, null, "ARP main FD");

    [Fact]
    public async Task Posts_Dr_FixedDeposit_Cr_CashOrBank_Resolved_To_The_Funding_Account()
    {
        FinancialAccount account = SetUpFundingAccount();

        FixedDepositDto result = await CreateHandler().Handle(ValidCommand(account.Id.Value), CancellationToken.None);

        result.CertificateNumber.Should().Be("FD-001");
        result.Status.Should().Be(nameof(FixedDepositStatus.Active));

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "FixedDepositPlacement" &&
                r.Lines.Count == 2 &&
                r.Lines.Any(l => l.Role == LedgerAccountType.FixedDeposit && l.Direction == LedgerDirection.Debit && l.Amount == 500_000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Credit &&
                    l.Amount == 500_000m && l.ExplicitAccountId == account.ChartOfAccountId)),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Certificate_Number_Already_Exists()
    {
        FinancialAccount account = SetUpFundingAccount();
        _fixedDeposits.ExistsForCertificateNumberAsync(TenantId, "FD-001", Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(account.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Funding_Account_Is_Inactive()
    {
        FinancialAccount account = SetUpFundingAccount(isActive: false);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(account.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Funding_Account_Belongs_To_A_Different_Tenant()
    {
        FinancialAccount account = FinancialAccount.Create(
            Guid.NewGuid(), "Other Tenant Bank", FinancialAccountType.Bank, null, null, null, ChartOfAccountId.New(),
            null, null);
        _financialAccounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(account.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
