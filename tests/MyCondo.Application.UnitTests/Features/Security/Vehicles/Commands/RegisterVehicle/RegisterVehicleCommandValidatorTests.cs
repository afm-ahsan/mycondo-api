using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.Vehicles.Commands.RegisterVehicle;

namespace MyCondo.Application.UnitTests.Features.Security.Vehicles.Commands.RegisterVehicle;

public class RegisterVehicleCommandValidatorTests
{
    private readonly RegisterVehicleCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        RegisterVehicleCommand command = new("ABC123", "Car", "Toyota", "Corolla", "White", "Resident", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_RegistrationNumber_Fails()
    {
        RegisterVehicleCommand command = new("", "Car", null, null, null, "Resident", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterVehicleCommand.RegistrationNumber));
    }

    [Fact]
    public void Invalid_VehicleType_Fails()
    {
        RegisterVehicleCommand command = new("ABC123", "NotAType", null, null, null, "Resident", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterVehicleCommand.VehicleType));
    }

    [Fact]
    public void Invalid_OwnershipCategory_Fails()
    {
        RegisterVehicleCommand command = new("ABC123", "Car", null, null, null, "NotACategory", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterVehicleCommand.OwnershipCategory));
    }
}
