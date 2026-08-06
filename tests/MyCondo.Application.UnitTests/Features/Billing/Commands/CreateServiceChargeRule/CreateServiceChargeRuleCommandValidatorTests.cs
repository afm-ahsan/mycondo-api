using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Billing.Commands.CreateServiceChargeRule;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.CreateServiceChargeRule;

public class CreateServiceChargeRuleCommandValidatorTests
{
    private readonly CreateServiceChargeRuleCommandValidator _validator = new();
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateServiceChargeRuleCommand command = new(
            Guid.NewGuid(), "ServiceCharge", "Standard Charge", "FixedAmount", 1500m, null, "Monthly", EffectiveFrom);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Command_With_UnitTypeFilter_Passes()
    {
        CreateServiceChargeRuleCommand command = new(
            Guid.NewGuid(), "ServiceCharge", "Standard Charge", "PerSquareFoot", 2.5m, "Residential", "Monthly", EffectiveFrom);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        CreateServiceChargeRuleCommand command = new(
            Guid.Empty, "ServiceCharge", "Standard Charge", "FixedAmount", 1500m, null, "Monthly", EffectiveFrom);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateServiceChargeRuleCommand.BuildingId));
    }

    [Fact]
    public void Zero_Rate_Fails()
    {
        CreateServiceChargeRuleCommand command = new(
            Guid.NewGuid(), "ServiceCharge", "Standard Charge", "FixedAmount", 0m, null, "Monthly", EffectiveFrom);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateServiceChargeRuleCommand.Rate));
    }

    [Fact]
    public void Invalid_CalculationMethod_Fails()
    {
        CreateServiceChargeRuleCommand command = new(
            Guid.NewGuid(), "ServiceCharge", "Standard Charge", "PerUnit", 1500m, null, "Monthly", EffectiveFrom);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateServiceChargeRuleCommand.CalculationMethod));
    }

    [Fact]
    public void Invalid_Frequency_Fails()
    {
        CreateServiceChargeRuleCommand command = new(
            Guid.NewGuid(), "ServiceCharge", "Standard Charge", "FixedAmount", 1500m, null, "Weekly", EffectiveFrom);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateServiceChargeRuleCommand.Frequency));
    }

    [Fact]
    public void Invalid_UnitTypeFilter_Fails()
    {
        CreateServiceChargeRuleCommand command = new(
            Guid.NewGuid(), "ServiceCharge", "Standard Charge", "FixedAmount", 1500m, "NotAType", "Monthly", EffectiveFrom);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateServiceChargeRuleCommand.UnitTypeFilter));
    }
}
