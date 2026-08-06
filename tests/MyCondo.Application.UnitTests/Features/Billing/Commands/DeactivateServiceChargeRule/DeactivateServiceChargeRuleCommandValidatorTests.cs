using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Billing.Commands.DeactivateServiceChargeRule;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.DeactivateServiceChargeRule;

public class DeactivateServiceChargeRuleCommandValidatorTests
{
    private readonly DeactivateServiceChargeRuleCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DeactivateServiceChargeRuleCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ServiceChargeRuleId_Fails()
    {
        DeactivateServiceChargeRuleCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeactivateServiceChargeRuleCommand.ServiceChargeRuleId));
    }
}
