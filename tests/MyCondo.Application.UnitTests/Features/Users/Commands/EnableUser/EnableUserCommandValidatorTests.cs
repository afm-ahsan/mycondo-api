using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Users.Commands.EnableUser;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.EnableUser;

public class EnableUserCommandValidatorTests
{
    private readonly EnableUserCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        EnableUserCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        EnableUserCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EnableUserCommand.UserId));
    }
}
