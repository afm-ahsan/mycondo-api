using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Residents.Commands.UpdateResident;

namespace MyCondo.Application.UnitTests.Features.Residents.Commands.UpdateResident;

public class UpdateResidentCommandValidatorTests
{
    private readonly UpdateResidentCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        UpdateResidentCommand command = new(Guid.NewGuid(), "Full Name", "+8801700000000", "resident@example.com");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FullName_Fails()
    {
        UpdateResidentCommand command = new(Guid.NewGuid(), string.Empty, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateResidentCommand.FullName));
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        UpdateResidentCommand command = new(Guid.NewGuid(), "Full Name", null, "not-an-email");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateResidentCommand.Email));
    }
}
