using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.Guests.Commands.CreateGuestProfile;

namespace MyCondo.Application.UnitTests.Features.Security.Guests.Commands.CreateGuestProfile;

public class CreateGuestProfileCommandValidatorTests
{
    private readonly CreateGuestProfileCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateGuestProfileCommand command = new("Jane Doe", "01700000000", "NID", "1234567890");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FullName_Fails()
    {
        CreateGuestProfileCommand command = new("", "01700000000", null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGuestProfileCommand.FullName));
    }

    [Fact]
    public void Empty_Phone_Fails()
    {
        CreateGuestProfileCommand command = new("Jane Doe", "", null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGuestProfileCommand.Phone));
    }
}
