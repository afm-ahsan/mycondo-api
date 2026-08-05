using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.ClockIn;

namespace MyCondo.Application.UnitTests.Features.Payroll.AttendanceRecords.Commands.ClockIn;

public class ClockInCommandValidatorTests
{
    private readonly ClockInCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DateOnly workDate = DateOnly.FromDateTime(DateTime.UtcNow);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ClockInCommand command = new(Guid.NewGuid(), workDate, now, now.AddHours(8), "Main Gate", "Manual");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Source_Fails()
    {
        DateOnly workDate = DateOnly.FromDateTime(DateTime.UtcNow);
        ClockInCommand command = new(Guid.NewGuid(), workDate, null, null, null, "NotASource");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ClockInCommand.Source));
    }

    [Fact]
    public void ScheduledEnd_Before_ScheduledStart_Fails()
    {
        DateOnly workDate = DateOnly.FromDateTime(DateTime.UtcNow);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ClockInCommand command = new(Guid.NewGuid(), workDate, now, now.AddHours(-1), null, "Manual");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ClockInCommand.ScheduledEndUtc));
    }
}
