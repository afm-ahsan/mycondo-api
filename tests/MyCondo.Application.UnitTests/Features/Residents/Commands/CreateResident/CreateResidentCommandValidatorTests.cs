using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Residents.Commands.CreateResident;

namespace MyCondo.Application.UnitTests.Features.Residents.Commands.CreateResident;

public class CreateResidentCommandValidatorTests
{
    private readonly CreateResidentCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateResidentCommand command = new(Guid.NewGuid(), "Jane Doe", "01700000000", "jane@example.com", "Owner");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Command_With_Null_Contact_Fields_Passes()
    {
        CreateResidentCommand command = new(Guid.NewGuid(), "Jane Doe", null, null, "Owner");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FullName_Fails()
    {
        CreateResidentCommand command = new(Guid.NewGuid(), "", null, null, "Owner");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateResidentCommand.FullName));
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        CreateResidentCommand command = new(Guid.NewGuid(), "Jane Doe", null, "not-an-email", "Owner");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateResidentCommand.Email));
    }

    [Fact]
    public void Invalid_ResidentType_Fails()
    {
        CreateResidentCommand command = new(Guid.NewGuid(), "Jane Doe", null, null, "NotARealType");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateResidentCommand.ResidentType));
    }
}
