using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Auth.Commands.UpdateMyProfile;

namespace MyCondo.Application.UnitTests.Features.Auth.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandValidatorTests
{
    private readonly UpdateMyProfileCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        UpdateMyProfileCommand command = new("Jane Doe", "01700000000");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Command_With_Null_Phone_Passes()
    {
        UpdateMyProfileCommand command = new("Jane Doe", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FullName_Fails()
    {
        UpdateMyProfileCommand command = new("", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMyProfileCommand.FullName));
    }

    [Fact]
    public void Overlong_PhoneNumber_Fails()
    {
        UpdateMyProfileCommand command = new("Jane Doe", new string('1', 41));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMyProfileCommand.PhoneNumber));
    }
}
