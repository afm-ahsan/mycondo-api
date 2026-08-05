using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.SebaVisits.Commands.CheckInSebaVisitor;

namespace MyCondo.Application.UnitTests.Features.Security.SebaVisits.Commands.CheckInSebaVisitor;

public class CheckInSebaVisitorCommandValidatorTests
{
    private readonly CheckInSebaVisitorCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CheckInSebaVisitorCommand command = new(
            "Jane Public", "017", "ABC Corp", "Complaints Dept", "T-001", "Complaint", Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_VisitorFullName_Fails()
    {
        CheckInSebaVisitorCommand command = new("", null, null, null, null, null, null, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckInSebaVisitorCommand.VisitorFullName));
    }

    [Fact]
    public void Empty_EntryGateId_Fails()
    {
        CheckInSebaVisitorCommand command = new("Jane Public", null, null, null, null, null, null, Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckInSebaVisitorCommand.EntryGateId));
    }
}
