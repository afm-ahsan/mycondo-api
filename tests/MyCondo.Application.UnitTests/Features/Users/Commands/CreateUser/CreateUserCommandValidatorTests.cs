using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Users.Commands.CreateUser;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateUserCommand command = new("Full Name", "user@example.com", "+8801700000000", "Str0ngPassw0rd!", true);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        CreateUserCommand command = new("Full Name", "not-an-email", "+8801700000000", "Str0ngPassw0rd!", true);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Fact]
    public void Missing_Phone_Number_Fails()
    {
        CreateUserCommand command = new("Full Name", "user@example.com", null, "Str0ngPassw0rd!", true);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.PhoneNumber));
    }

    [Theory]
    [InlineData("Sh0!")]
    [InlineData("alllowercase12!")]
    [InlineData("ALLUPPERCASE12!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecialChar12")]
    public void Weak_Password_Fails(string weakPassword)
    {
        CreateUserCommand command = new("Full Name", "user@example.com", "+8801700000000", weakPassword, true);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Password));
    }

    [Fact]
    public void Missing_Password_Fails()
    {
        CreateUserCommand command = new("Full Name", "user@example.com", "+8801700000000", "", true);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Password));
    }

    [Fact]
    public void Strong_Password_Passes()
    {
        CreateUserCommand command = new("Full Name", "user@example.com", "+8801700000000", "Str0ngPassw0rd!", true);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
