using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Commands.EndRatePlanPeriod;

namespace MyCondo.Application.UnitTests.Features.Utilities.Commands.EndRatePlanPeriod;

public class EndRatePlanPeriodCommandValidatorTests
{
    private readonly EndRatePlanPeriodCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        EndRatePlanPeriodCommand command = new(Guid.NewGuid(), new DateOnly(2026, 12, 31));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_RatePlanId_Fails()
    {
        EndRatePlanPeriodCommand command = new(Guid.Empty, new DateOnly(2026, 12, 31));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EndRatePlanPeriodCommand.RatePlanId));
    }
}
