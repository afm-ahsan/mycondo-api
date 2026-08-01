using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Users.Commands.DeactivateUser;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.DeactivateUser;

public class DeactivateUserCommandValidatorTests
{
    private readonly DeactivateUserCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DeactivateUserCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        DeactivateUserCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeactivateUserCommand.UserId));
    }
}
