using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Users.Commands.CreateUser;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Without_A_Password_Passes()
    {
        CreateUserCommand command = new("Full Name", "user@example.com", "+8801000000000", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        CreateUserCommand command = new("Full Name", "not-an-email", null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase12")]
    [InlineData("ALLUPPERCASE12")]
    [InlineData("NoDigitsHere")]
    public void Weak_Supplied_Password_Fails(string weakPassword)
    {
        CreateUserCommand command = new("Full Name", "user@example.com", null, weakPassword);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.InitialPassword));
    }

    [Fact]
    public void Strong_Supplied_Password_Passes()
    {
        CreateUserCommand command = new("Full Name", "user@example.com", null, "Str0ngPassw0rd!");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
