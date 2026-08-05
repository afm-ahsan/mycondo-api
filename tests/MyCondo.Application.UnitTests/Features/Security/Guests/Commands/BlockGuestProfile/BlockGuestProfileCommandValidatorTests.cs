using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.Guests.Commands.BlockGuestProfile;

namespace MyCondo.Application.UnitTests.Features.Security.Guests.Commands.BlockGuestProfile;

public class BlockGuestProfileCommandValidatorTests
{
    private readonly BlockGuestProfileCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        BlockGuestProfileCommand command = new(Guid.NewGuid(), "Reported theft");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Reason_Fails()
    {
        BlockGuestProfileCommand command = new(Guid.NewGuid(), "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BlockGuestProfileCommand.Reason));
    }
}
