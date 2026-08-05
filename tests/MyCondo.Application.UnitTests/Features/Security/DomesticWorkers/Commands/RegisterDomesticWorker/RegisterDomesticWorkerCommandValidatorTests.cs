using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.DomesticWorkers.Commands.RegisterDomesticWorker;

namespace MyCondo.Application.UnitTests.Features.Security.DomesticWorkers.Commands.RegisterDomesticWorker;

public class RegisterDomesticWorkerCommandValidatorTests
{
    private readonly RegisterDomesticWorkerCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        RegisterDomesticWorkerCommand command = new("Jane Doe", "01700000000", "Maid", null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_WorkerType_Fails()
    {
        RegisterDomesticWorkerCommand command = new("Jane Doe", "01700000000", "NotAType", null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterDomesticWorkerCommand.WorkerType));
    }

    [Fact]
    public void Empty_Phone_Fails()
    {
        RegisterDomesticWorkerCommand command = new("Jane Doe", "", "Maid", null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterDomesticWorkerCommand.Phone));
    }
}
