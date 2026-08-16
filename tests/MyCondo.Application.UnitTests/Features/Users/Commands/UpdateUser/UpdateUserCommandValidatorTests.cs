using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Users.Commands.UpdateUser;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        UpdateUserCommand command = new(Guid.NewGuid(), "Full Name", "+8801700000000");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FullName_Fails()
    {
        UpdateUserCommand command = new(Guid.NewGuid(), string.Empty, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.FullName));
    }
}
