using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Leasing.Commands.CreateOccupancyRegistration;

namespace MyCondo.Application.UnitTests.Features.Leasing.Commands.CreateOccupancyRegistration;

public class CreateOccupancyRegistrationCommandValidatorTests
{
    private readonly CreateOccupancyRegistrationCommandValidator _validator = new();

    private static CreateOccupancyRegistrationCommand ValidCommand() => new(
        Guid.NewGuid(), "Occupant", "Jane Doe", "01700000000", null, null, "1234567890", null, "Female", null,
        "Islam", "Bangladeshi", "Robert Doe", "Mary Doe", "Married", "Engineer", null, null,
        "123 Example Road, Dhaka", null, null, null);

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_PrimaryEmail_Passes()
    {
        CreateOccupancyRegistrationCommand command = ValidCommand() with { PrimaryEmail = null };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_PrimaryEmail_Fails()
    {
        CreateOccupancyRegistrationCommand command = ValidCommand() with { PrimaryEmail = "not-an-email" };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOccupancyRegistrationCommand.PrimaryEmail));
    }
}
