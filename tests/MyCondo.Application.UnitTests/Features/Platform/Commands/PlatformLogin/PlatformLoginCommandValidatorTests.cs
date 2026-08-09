using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Platform.Commands.PlatformLogin;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.PlatformLogin;

public class PlatformLoginCommandValidatorTests
{
    private readonly PlatformLoginCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        PlatformLoginCommand command = new("sadmin@mycondo.com", "correct-horse-battery-staple");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_Email_Fails(string email)
    {
        PlatformLoginCommand command = new(email, "password");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PlatformLoginCommand.Email));
    }

    [Fact]
    public void Empty_Password_Fails()
    {
        PlatformLoginCommand command = new("sadmin@mycondo.com", "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PlatformLoginCommand.Password));
    }

    [Fact]
    public void Command_Has_No_Tenant_Or_Organization_Field()
    {
        // Compile-time proof, not just a validation-rule proof: there is no property to add a rule
        // for in the first place. See mycondo-docs ADR-019.
        typeof(PlatformLoginCommand).GetProperties().Select(p => p.Name).Should().NotContain(
            name => name.Contains("Tenant", StringComparison.OrdinalIgnoreCase)
                 || name.Contains("Organization", StringComparison.OrdinalIgnoreCase));
    }
}
