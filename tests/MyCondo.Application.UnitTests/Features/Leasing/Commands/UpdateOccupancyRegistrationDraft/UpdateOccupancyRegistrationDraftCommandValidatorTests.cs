using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Leasing.Commands.UpdateOccupancyRegistrationDraft;

namespace MyCondo.Application.UnitTests.Features.Leasing.Commands.UpdateOccupancyRegistrationDraft;

public class UpdateOccupancyRegistrationDraftCommandValidatorTests
{
    private readonly UpdateOccupancyRegistrationDraftCommandValidator _validator = new();

    private static UpdateOccupancyRegistrationDraftCommand ValidCommand() => new(
        Guid.NewGuid(), "Jane Doe", "01700000000", null, "1234567890", null, "Female", null, "Islam",
        "Bangladeshi", "Robert Doe", "Mary Doe", "Married", "Engineer", "123 Example Road, Dhaka", null, null,
        null);

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void PrimaryPhone_Blank_Fails(string? phone)
    {
        UpdateOccupancyRegistrationDraftCommand command = ValidCommand() with { PrimaryPhone = phone };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOccupancyRegistrationDraftCommand.PrimaryPhone));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PrimaryReligion_Blank_Fails(string? religion)
    {
        UpdateOccupancyRegistrationDraftCommand command = ValidCommand() with { PrimaryReligion = religion };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOccupancyRegistrationDraftCommand.PrimaryReligion));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PrimaryNationality_Blank_Fails(string? nationality)
    {
        UpdateOccupancyRegistrationDraftCommand command = ValidCommand() with { PrimaryNationality = nationality };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOccupancyRegistrationDraftCommand.PrimaryNationality));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PrimaryFatherName_Blank_Fails(string? fatherName)
    {
        UpdateOccupancyRegistrationDraftCommand command = ValidCommand() with { PrimaryFatherName = fatherName };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOccupancyRegistrationDraftCommand.PrimaryFatherName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PrimaryMotherName_Blank_Fails(string? motherName)
    {
        UpdateOccupancyRegistrationDraftCommand command = ValidCommand() with { PrimaryMotherName = motherName };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOccupancyRegistrationDraftCommand.PrimaryMotherName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PrimaryMaritalStatus_Blank_Fails(string? maritalStatus)
    {
        UpdateOccupancyRegistrationDraftCommand command = ValidCommand() with { PrimaryMaritalStatus = maritalStatus };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOccupancyRegistrationDraftCommand.PrimaryMaritalStatus));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PrimaryProfession_Blank_Fails(string? profession)
    {
        UpdateOccupancyRegistrationDraftCommand command = ValidCommand() with { PrimaryProfession = profession };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOccupancyRegistrationDraftCommand.PrimaryProfession));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PrimaryPermanentAddress_Blank_Fails(string? permanentAddress)
    {
        UpdateOccupancyRegistrationDraftCommand command = ValidCommand() with { PrimaryPermanentAddress = permanentAddress };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOccupancyRegistrationDraftCommand.PrimaryPermanentAddress));
    }
}
