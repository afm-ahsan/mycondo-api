using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.SaveOwnerResidentProfile;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Commands.SaveOwnerResidentProfile;

public class SaveOwnerResidentProfileCommandValidatorTests
{
    private readonly SaveOwnerResidentProfileCommandValidator _validator = new();

    private static SaveOwnerResidentProfileCommand ValidCommand() => new(
        Guid.NewGuid(), "Jane Owner", "01700000000", "jane@example.com", null, "1234567890", null,
        DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30), "Female", "Present Addr", "Permanent Addr",
        "Father Name", "Mother Name", "Single", "Engineer", null, null, null, null, null, "Islam", "Bangladeshi");

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Email_Passes()
    {
        SaveOwnerResidentProfileCommand command = ValidCommand() with { Email = null };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        SaveOwnerResidentProfileCommand command = ValidCommand() with { Email = "not-an-email" };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SaveOwnerResidentProfileCommand.Email));
    }
}
