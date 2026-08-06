using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Commands.CorrectReading;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings;
using MyCondo.Domain.Features.Utilities.Readings.Exceptions;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.CorrectReading;

public class CorrectReadingCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly MeterId MeterId = MeterId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 3, 31);

    private readonly IReadingRepository _readings = Substitute.For<IReadingRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly ILedgerPostingRepository _ledgerPostings = Substitute.For<ILedgerPostingRepository>();
    private readonly ILedgerEntryRepository _ledgerEntries = Substitute.For<ILedgerEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CorrectReadingCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private CorrectReadingCommandHandler CreateHandler() => new(
        _readings, _invoices, _ledgerPostings, _ledgerEntries, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CorrectReadingCommandHandler>>());

    private static Reading FinalizedReading(decimal previous = 0m, decimal present = 50m)
    {
        Reading reading = Reading.Record(
            TenantId, MeterId, FlatId, UtilityType.Electricity, BuildingId, PeriodStart, PeriodEnd, previous, present,
            PeriodEnd, null, null, Now);
        reading.Review(null, Now);
        reading.Finalize(null, false, null, Now);
        return reading;
    }

    [Fact]
    public async Task Correcting_A_Finalized_Reading_Does_Not_Touch_Any_Invoice()
    {
        Reading original = FinalizedReading();
        _readings.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);

        CorrectReadingCommand command = new(original.Id.Value, 0m, 55m, PeriodEnd, null, "Transcription error");
        ReadingDto result = await CreateHandler().Handle(command, CancellationToken.None);

        original.Status.Should().Be(ReadingStatus.Corrected);
        result.CorrectsReadingId.Should().Be(original.Id.Value);
        result.Status.Should().Be(ReadingStatus.Draft.ToString());
        await _invoices.DidNotReceive().GetByIdAsync(Arg.Any<InvoiceId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Correcting_A_Billed_Reading_Voids_Its_Invoice()
    {
        Reading original = FinalizedReading();
        InvoiceId invoiceId = InvoiceId.New();
        original.MarkBilled(invoiceId, null, Now);
        _readings.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);

        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, "INV-AISHA-2026-000001", InvoiceSource.Utility, PeriodStart, PeriodEnd,
            PeriodEnd, PeriodEnd,
            [new InvoiceLineInput(null, "Standard Electricity", "Electricity", "Fixed", 800m, null, 1m, 800m, "Fixed charge")],
            LedgerPostingId.New(), Now);
        _invoices.GetByIdAsync(invoiceId, Arg.Any<CancellationToken>()).Returns(invoice);

        CorrectReadingCommand command = new(original.Id.Value, 0m, 55m, PeriodEnd, null, "Meter misread");
        await CreateHandler().Handle(command, CancellationToken.None);

        invoice.Status.Should().Be(InvoiceStatus.Void);
        invoice.VoidReason.Should().Be("Meter misread");
        _ledgerPostings.Received(1).Add(Arg.Any<LedgerPosting>());
    }

    [Fact]
    public async Task Throws_When_Reading_Is_Draft()
    {
        Reading draftReading = Reading.Record(
            TenantId, MeterId, FlatId, UtilityType.Electricity, BuildingId, PeriodStart, PeriodEnd, 0m, 50m,
            PeriodEnd, null, null, Now);
        _readings.GetByIdAsync(draftReading.Id, Arg.Any<CancellationToken>()).Returns(draftReading);

        CorrectReadingCommand command = new(draftReading.Id.Value, 0m, 55m, PeriodEnd, null, "Attempted correction");
        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ReadingInvalidTransitionException>();
    }
}
