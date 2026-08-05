using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Commands.RequestAttendanceCorrection;

namespace MyCondo.Application.UnitTests.Features.Payroll.AttendanceRecords.Commands.RequestAttendanceCorrection;

public class RequestAttendanceCorrectionCommandValidatorTests
{
    private readonly RequestAttendanceCorrectionCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        RequestAttendanceCorrectionCommand command = new(Guid.NewGuid(), "Forgot to clock in");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Reason_Fails()
    {
        RequestAttendanceCorrectionCommand command = new(Guid.NewGuid(), "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestAttendanceCorrectionCommand.Reason));
    }
}
