using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.DeactivateRatePlan;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.DeactivateRatePlan;

public class DeactivateRatePlanCommandValidatorTests
{
    private readonly DeactivateRatePlanCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        DeactivateRatePlanCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_RatePlanId_Fails()
    {
        DeactivateRatePlanCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeactivateRatePlanCommand.RatePlanId));
    }
}
