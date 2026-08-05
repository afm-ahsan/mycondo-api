using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutGuest;

namespace MyCondo.Application.UnitTests.Features.Security.AccessSessions.Commands.CheckOutGuest;

public class CheckOutGuestCommandValidatorTests
{
    private readonly CheckOutGuestCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CheckOutGuestCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_AccessSessionId_Fails()
    {
        CheckOutGuestCommand command = new(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckOutGuestCommand.AccessSessionId));
    }
}
