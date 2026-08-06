using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.CreateRatePlan;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.CreateRatePlan;

public class CreateRatePlanCommandValidatorTests
{
    private readonly CreateRatePlanCommandValidator _validator = new();
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    private static readonly RateSlabInputDto[] Slabs = [new(1, 0m, null, 5m)];

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateRatePlanCommand command = new(
            Guid.NewGuid(), "Electricity", "Standard Electricity", "Metered", null, 50m, 0m, EffectiveFrom, Slabs);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        CreateRatePlanCommand command = new(
            Guid.Empty, "Electricity", "Standard Electricity", "Metered", null, 50m, 0m, EffectiveFrom, Slabs);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRatePlanCommand.BuildingId));
    }

    [Fact]
    public void Invalid_UtilityType_Fails()
    {
        CreateRatePlanCommand command = new(
            Guid.NewGuid(), "Water", "Standard Electricity", "Metered", null, 50m, 0m, EffectiveFrom, Slabs);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRatePlanCommand.UtilityType));
    }

    [Fact]
    public void Invalid_Structure_Fails()
    {
        CreateRatePlanCommand command = new(
            Guid.NewGuid(), "Electricity", "Standard Electricity", "Tiered", null, 50m, 0m, EffectiveFrom, Slabs);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRatePlanCommand.Structure));
    }

    [Fact]
    public void Negative_TaxPercentage_Fails()
    {
        CreateRatePlanCommand command = new(
            Guid.NewGuid(), "Electricity", "Standard Electricity", "Metered", null, 50m, -1m, EffectiveFrom, Slabs);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRatePlanCommand.TaxPercentage));
    }
}
