using AwesomeAssertions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings;
using MyCondo.Domain.Features.Utilities.Readings.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Utilities.Readings;

public class ReadingTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly MeterId MeterId = MeterId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 3, 31);

    private static Reading RecordReading(decimal previous = 100m, decimal present = 250m, string? overrideReason = null) =>
        Reading.Record(
            TenantId, MeterId, FlatId, UtilityType.Electricity, BuildingId, PeriodStart, PeriodEnd, previous, present,
            PeriodEnd, overrideReason, null, Now);

    [Fact]
    public void Record_Starts_Draft_And_Computes_Consumption()
    {
        Reading reading = RecordReading(100m, 250m);

        reading.Status.Should().Be(ReadingStatus.Draft);
        reading.ConsumptionUnits.Should().Be(150m);
        reading.Version.Should().Be(1);
    }

    [Fact]
    public void Record_Throws_When_Present_Below_Previous_Without_Override()
    {
        Action act = () => RecordReading(250m, 100m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_Allows_Present_Below_Previous_With_Override_Reason()
    {
        Reading reading = RecordReading(250m, 100m, "Meter was reset after replacement");

        reading.ConsumptionUnits.Should().Be(-150m);
        reading.OverrideReason.Should().Be("Meter was reset after replacement");
    }

    [Fact]
    public void Review_Transitions_Draft_To_Reviewed()
    {
        Reading reading = RecordReading();
        Guid reviewer = Guid.NewGuid();

        reading.Review(reviewer, Now.AddHours(1));

        reading.Status.Should().Be(ReadingStatus.Reviewed);
        reading.ReviewedBy.Should().Be(reviewer);
        reading.ReviewedAtUtc.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void Review_Throws_When_Not_Draft()
    {
        Reading reading = RecordReading();
        reading.Review(Guid.NewGuid(), Now);

        Action act = () => reading.Review(Guid.NewGuid(), Now.AddHours(1));

        act.Should().Throw<ReadingInvalidTransitionException>();
    }

    [Fact]
    public void Finalize_Transitions_Reviewed_To_Finalized()
    {
        Reading reading = RecordReading();
        reading.Review(Guid.NewGuid(), Now);

        reading.Finalize(Guid.NewGuid(), true, "Consumption spike", Now.AddHours(2));

        reading.Status.Should().Be(ReadingStatus.Finalized);
        reading.IsAbnormalConsumption.Should().BeTrue();
        reading.AbnormalConsumptionReason.Should().Be("Consumption spike");
    }

    [Fact]
    public void Finalize_Throws_When_Not_Reviewed()
    {
        Reading reading = RecordReading();

        Action act = () => reading.Finalize(Guid.NewGuid(), false, null, Now);

        act.Should().Throw<ReadingInvalidTransitionException>();
    }

    [Fact]
    public void MarkBilled_Transitions_Finalized_To_Billed()
    {
        Reading reading = RecordReading();
        reading.Review(Guid.NewGuid(), Now);
        reading.Finalize(Guid.NewGuid(), false, null, Now);
        InvoiceId invoiceId = InvoiceId.New();

        reading.MarkBilled(invoiceId, Guid.NewGuid(), Now.AddHours(3));

        reading.Status.Should().Be(ReadingStatus.Billed);
        reading.InvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public void MarkBilled_Throws_When_Not_Finalized()
    {
        Reading reading = RecordReading();

        Action act = () => reading.MarkBilled(InvoiceId.New(), Guid.NewGuid(), Now);

        act.Should().Throw<ReadingInvalidTransitionException>();
    }

    [Fact]
    public void MarkCorrected_Succeeds_From_Finalized()
    {
        Reading reading = RecordReading();
        reading.Review(Guid.NewGuid(), Now);
        reading.Finalize(Guid.NewGuid(), false, null, Now);

        reading.MarkCorrected(Now.AddHours(4));

        reading.Status.Should().Be(ReadingStatus.Corrected);
    }

    [Fact]
    public void MarkCorrected_Succeeds_From_Billed()
    {
        Reading reading = RecordReading();
        reading.Review(Guid.NewGuid(), Now);
        reading.Finalize(Guid.NewGuid(), false, null, Now);
        reading.MarkBilled(InvoiceId.New(), Guid.NewGuid(), Now);

        reading.MarkCorrected(Now.AddHours(5));

        reading.Status.Should().Be(ReadingStatus.Corrected);
    }

    [Fact]
    public void MarkCorrected_Throws_When_Draft()
    {
        Reading reading = RecordReading();

        Action act = () => reading.MarkCorrected(Now);

        act.Should().Throw<ReadingInvalidTransitionException>();
    }

    [Fact]
    public void Record_Sets_CorrectsReadingId_When_Provided()
    {
        ReadingId originalId = ReadingId.New();

        Reading correction = Reading.Record(
            TenantId, MeterId, FlatId, UtilityType.Electricity, BuildingId, PeriodStart, PeriodEnd, 250m, 300m,
            PeriodEnd, null, originalId, Now);

        correction.CorrectsReadingId.Should().Be(originalId);
    }
}
