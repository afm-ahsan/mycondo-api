using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.CreateDomesticWorkerAssignment;

namespace MyCondo.Application.UnitTests.Features.Security.DomesticWorkerAssignments.Commands.CreateDomesticWorkerAssignment;

public class CreateDomesticWorkerAssignmentCommandValidatorTests
{
    private readonly CreateDomesticWorkerAssignmentCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        CreateDomesticWorkerAssignmentCommand command = new(
            Guid.NewGuid(), Guid.NewGuid(), now, now.AddYears(1), "Monday,Tuesday", null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_AllowedDays_Passes()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        CreateDomesticWorkerAssignmentCommand command = new(Guid.NewGuid(), Guid.NewGuid(), now, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_AllowedDays_Fails()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        CreateDomesticWorkerAssignmentCommand command = new(Guid.NewGuid(), Guid.NewGuid(), now, null, "NotADay", null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDomesticWorkerAssignmentCommand.AllowedDays));
    }

    [Fact]
    public void ValidToUtc_Before_ValidFromUtc_Fails()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        CreateDomesticWorkerAssignmentCommand command = new(
            Guid.NewGuid(), Guid.NewGuid(), now, now.AddDays(-1), null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDomesticWorkerAssignmentCommand.ValidToUtc));
    }
}
