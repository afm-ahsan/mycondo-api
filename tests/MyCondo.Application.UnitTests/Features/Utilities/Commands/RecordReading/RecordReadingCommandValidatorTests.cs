using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.RecordReading;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.RecordReading;

public class RecordReadingCommandValidatorTests
{
    private readonly RecordReadingCommandValidator _validator = new();
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 3, 31);

    [Fact]
    public void Valid_Command_Passes()
    {
        RecordReadingCommand command = new(Guid.NewGuid(), PeriodStart, PeriodEnd, 100m, 250m, PeriodEnd, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_MeterId_Fails()
    {
        RecordReadingCommand command = new(Guid.Empty, PeriodStart, PeriodEnd, 100m, 250m, PeriodEnd, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordReadingCommand.MeterId));
    }

    [Fact]
    public void PeriodEnd_Before_PeriodStart_Fails()
    {
        RecordReadingCommand command = new(Guid.NewGuid(), PeriodEnd, PeriodStart, 100m, 250m, PeriodEnd, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordReadingCommand.PeriodEnd));
    }

    [Fact]
    public void Negative_PresentReading_Fails()
    {
        RecordReadingCommand command = new(Guid.NewGuid(), PeriodStart, PeriodEnd, 100m, -1m, PeriodEnd, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordReadingCommand.PresentReading));
    }
}
