using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.DomesticWorkers.Commands.SetDomesticWorkerStatus;

namespace MyCondo.Application.UnitTests.Features.Security.DomesticWorkers.Commands.SetDomesticWorkerStatus;

public class SetDomesticWorkerStatusCommandValidatorTests
{
    private readonly SetDomesticWorkerStatusCommandValidator _validator = new();

    [Fact]
    public void Active_Status_Does_Not_Require_Reason()
    {
        SetDomesticWorkerStatusCommand command = new(Guid.NewGuid(), "Active", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Suspended_Status_Without_Reason_Fails()
    {
        SetDomesticWorkerStatusCommand command = new(Guid.NewGuid(), "Suspended", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetDomesticWorkerStatusCommand.Reason));
    }

    [Fact]
    public void Blocked_Status_With_Reason_Passes()
    {
        SetDomesticWorkerStatusCommand command = new(Guid.NewGuid(), "Blocked", "Theft reported");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Status_Fails()
    {
        SetDomesticWorkerStatusCommand command = new(Guid.NewGuid(), "NotAStatus", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetDomesticWorkerStatusCommand.Status));
    }
}
