using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Flats.Commands.UpdateFlat;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Commands.UpdateFlat;

public class UpdateFlatCommandValidatorTests
{
    private readonly UpdateFlatCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        UpdateFlatCommand command = new(Guid.NewGuid(), "A-101", 1, "Residential");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FlatId_Fails()
    {
        UpdateFlatCommand command = new(Guid.Empty, "A-101", 1, "Residential");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFlatCommand.FlatId));
    }

    [Fact]
    public void Empty_FlatNumber_Fails()
    {
        UpdateFlatCommand command = new(Guid.NewGuid(), "", 1, "Residential");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFlatCommand.FlatNumber));
    }

    [Fact]
    public void Invalid_FlatType_Fails()
    {
        UpdateFlatCommand command = new(Guid.NewGuid(), "A-101", 1, "NotAType");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateFlatCommand.FlatType));
    }

    [Fact]
    public void Null_FloorNumber_Is_Valid()
    {
        UpdateFlatCommand command = new(Guid.NewGuid(), "A-101", null, "Residential");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
