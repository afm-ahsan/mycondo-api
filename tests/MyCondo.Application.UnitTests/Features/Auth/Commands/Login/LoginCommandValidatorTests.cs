using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Auth.Commands.Login;

namespace MyCondo.Application.UnitTests.Features.Auth.Commands.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        LoginCommand command = new(Guid.NewGuid(), "someone@example.com", "correct-horse-battery-staple");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        LoginCommand command = new(Guid.Empty, "someone@example.com", "password");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.TenantId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_Email_Fails(string email)
    {
        LoginCommand command = new(Guid.NewGuid(), email, "password");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Empty_Password_Fails()
    {
        LoginCommand command = new(Guid.NewGuid(), "someone@example.com", "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}
