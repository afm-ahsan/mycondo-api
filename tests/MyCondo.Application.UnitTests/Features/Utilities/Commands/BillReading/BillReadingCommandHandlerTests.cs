using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Application.Features.Utilities.Commands.BillReading;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.RatePlans;
using MyCondo.Domain.Features.Utilities.Readings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.BillReading;

/// <summary>
/// Handler-level tests, same NSubstitute pattern as
/// RecordPaymentCommandHandlerFifoAllocationTests (Slice E) — proving BillReadingCommandHandler
/// wires UtilityCalculator's output into a correctly-sourced Invoice and transitions the reading to
/// Billed, without needing a real database.
/// </summary>
public class BillReadingCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly MeterId MeterId = MeterId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 3, 31);

    private readonly IReadingRepository _readings = Substitute.For<IReadingRepository>();
    private readonly IRatePlanRepository _ratePlans = Substitute.For<IRatePlanRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IInvoiceSequenceRepository _sequences = Substitute.For<IInvoiceSequenceRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public BillReadingCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IUnitOfWorkTransaction>());
        _sequences.GetNextValueAsync(TenantId, BuildingId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(1);
        _buildings.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>())
            .Returns(Building.Create(TenantId, "Aisha Tower", "AISHA", null, Now));
        StubFinancialPosting();
    }

    /// <summary>Makes the mocked <see cref="IFinancialPostingService"/> behave like the real one — see
    /// VoidInvoiceCommandHandlerTests for why.</summary>
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
                    request.SourceId, lines, Now);
                return new FinancialPostingResult(posting, entries);
            });

    private BillReadingCommandHandler CreateHandler() => new(
        _readings, _ratePlans, _buildings, _invoices, _sequences, _financialPosting, _unitOfWork,
        _currentUser, _clock, Substitute.For<ILogger<BillReadingCommandHandler>>());

    private static Reading FinalizedReading(decimal previous = 0m, decimal present = 50m)
    {
        Reading reading = Reading.Record(
            TenantId, MeterId, FlatId, UtilityType.Electricity, BuildingId, PeriodStart, PeriodEnd, previous, present,
            PeriodEnd, null, null, Now);
        reading.Review(null, Now);
        reading.Finalize(null, false, null, Now);
        return reading;
    }

    private static (RatePlan Plan, IReadOnlyList<RateSlab> Slabs) FixedPlan(decimal amount) =>
        RatePlan.Create(
            TenantId, BuildingId, UtilityType.Electricity, "Standard Electricity", RateStructure.Fixed, amount, 0m,
            0m, PeriodStart, [], Now);

    [Fact]
    public async Task Bills_Finalized_Reading_As_Utility_Invoice_And_Marks_Reading_Billed()
    {
        Reading reading = FinalizedReading();
        _readings.GetByIdAsync(reading.Id, Arg.Any<CancellationToken>()).Returns(reading);

        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = FixedPlan(800m);
        _ratePlans.GetApplicablePlanAsync(TenantId, BuildingId, UtilityType.Electricity, PeriodStart, PeriodEnd, Arg.Any<CancellationToken>())
            .Returns(plan);
        _ratePlans.GetSlabsForPlanAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(slabs);

        InvoiceDto result = await CreateHandler().Handle(new BillReadingCommand(reading.Id.Value), CancellationToken.None);

        result.Source.Should().Be("Utility");
        result.TotalAmount.Should().Be(800m);
        result.InvoiceNumber.Should().StartWith("INV-AISHA-");
        reading.Status.Should().Be(ReadingStatus.Billed);
        reading.InvoiceId.Should().NotBeNull();
    }

    [Fact]
    public async Task Throws_ConflictException_When_No_Applicable_RatePlan()
    {
        Reading reading = FinalizedReading();
        _readings.GetByIdAsync(reading.Id, Arg.Any<CancellationToken>()).Returns(reading);
        _ratePlans.GetApplicablePlanAsync(TenantId, BuildingId, UtilityType.Electricity, PeriodStart, PeriodEnd, Arg.Any<CancellationToken>())
            .Returns((RatePlan?)null);

        Func<Task> act = () => CreateHandler().Handle(new BillReadingCommand(reading.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_When_Reading_Is_Not_Finalized()
    {
        Reading draftReading = Reading.Record(
            TenantId, MeterId, FlatId, UtilityType.Electricity, BuildingId, PeriodStart, PeriodEnd, 0m, 50m,
            PeriodEnd, null, null, Now);
        _readings.GetByIdAsync(draftReading.Id, Arg.Any<CancellationToken>()).Returns(draftReading);

        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = FixedPlan(800m);
        _ratePlans.GetApplicablePlanAsync(TenantId, BuildingId, UtilityType.Electricity, PeriodStart, PeriodEnd, Arg.Any<CancellationToken>())
            .Returns(plan);
        _ratePlans.GetSlabsForPlanAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(slabs);

        Func<Task> act = () => CreateHandler().Handle(new BillReadingCommand(draftReading.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<Domain.Features.Utilities.Readings.Exceptions.ReadingInvalidTransitionException>();
    }
}
