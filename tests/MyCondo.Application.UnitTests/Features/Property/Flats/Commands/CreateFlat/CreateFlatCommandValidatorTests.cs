using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Flats.Commands.CreateFlat;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Commands.CreateFlat;

public class CreateFlatCommandValidatorTests
{
    private readonly CreateFlatCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateFlatCommand command = new(Guid.NewGuid(), "A-501", 5, "Residential");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        CreateFlatCommand command = new(Guid.Empty, "A-501", 5, "Residential");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFlatCommand.BuildingId));
    }

    [Fact]
    public void Empty_FlatNumber_Fails()
    {
        CreateFlatCommand command = new(Guid.NewGuid(), "", 5, "Residential");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFlatCommand.FlatNumber));
    }

    [Fact]
    public void Invalid_FlatType_Fails()
    {
        CreateFlatCommand command = new(Guid.NewGuid(), "A-501", 5, "NotARealType");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFlatCommand.FlatType));
    }
}
