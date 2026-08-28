using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Operations.Commands.CreateSupplier;

namespace MyCondo.Application.UnitTests.Features.Operations.Commands.CreateSupplier;

public class CreateSupplierCommandValidatorTests
{
    private readonly CreateSupplierCommandValidator _validator = new();

    private static CreateSupplierCommand ValidCommand() => new(
        "Acme Gas", "01700000000", "contact@example.com", "123 Example Road, Dhaka");

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ContactEmail_Passes()
    {
        CreateSupplierCommand command = ValidCommand() with { ContactEmail = null };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_ContactEmail_Fails()
    {
        CreateSupplierCommand command = ValidCommand() with { ContactEmail = "not-an-email" };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSupplierCommand.ContactEmail));
    }
}
