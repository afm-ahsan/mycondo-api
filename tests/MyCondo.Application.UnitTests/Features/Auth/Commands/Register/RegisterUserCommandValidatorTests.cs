using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Auth.Commands.Register;

namespace MyCondo.Application.UnitTests.Features.Auth.Commands.Register;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    private static RegisterUserCommand ValidCommand() => new(
        Guid.NewGuid(), "someone@example.com", "Correct-Horse-Battery-9", "Full Name", null);

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_Email_Fails(string email)
    {
        RegisterUserCommand command = ValidCommand() with { Email = email };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.Email));
    }

    [Fact]
    public void Email_With_Surrounding_Whitespace_Passes()
    {
        RegisterUserCommand command = ValidCommand() with { Email = "  someone@example.com  " };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
