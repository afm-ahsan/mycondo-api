using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.RegisterFlatOwner;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Commands.RegisterFlatOwner;

public class RegisterFlatOwnerCommandValidatorTests
{
    private readonly RegisterFlatOwnerCommandValidator _validator = new();

    private static RegisterFlatOwnerCommand ValidCommand() => new(
        Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), "Jane Owner", null, null, null, null, null, null,
        null, null, null, null, null, null, null, null, null, null, null);

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FlatId_Empty_Fails()
    {
        RegisterFlatOwnerCommand command = ValidCommand() with { FlatId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterFlatOwnerCommand.FlatId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FullName_Blank_Fails(string fullName)
    {
        RegisterFlatOwnerCommand command = ValidCommand() with { FullName = fullName };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterFlatOwnerCommand.FullName));
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        RegisterFlatOwnerCommand command = ValidCommand() with { Email = "not-an-email" };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterFlatOwnerCommand.Email));
    }
}
