using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Billing.Commands.EndServiceChargeRulePeriod;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.EndServiceChargeRulePeriod;

public class EndServiceChargeRulePeriodCommandValidatorTests
{
    private readonly EndServiceChargeRulePeriodCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        EndServiceChargeRulePeriodCommand command = new(Guid.NewGuid(), new DateOnly(2026, 6, 30));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ServiceChargeRuleId_Fails()
    {
        EndServiceChargeRulePeriodCommand command = new(Guid.Empty, new DateOnly(2026, 6, 30));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EndServiceChargeRulePeriodCommand.ServiceChargeRuleId));
    }

    [Fact]
    public void Default_EffectiveTo_Fails()
    {
        EndServiceChargeRulePeriodCommand command = new(Guid.NewGuid(), default);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EndServiceChargeRulePeriodCommand.EffectiveTo));
    }
}
