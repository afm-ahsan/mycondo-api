using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payments.Commands.OpenResidentAccount;

namespace MyCondo.Application.UnitTests.Features.Payments.Commands.OpenResidentAccount;

public class OpenResidentAccountCommandValidatorTests
{
    private readonly OpenResidentAccountCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        OpenResidentAccountCommand command = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FlatId_Fails()
    {
        OpenResidentAccountCommand command = new(Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpenResidentAccountCommand.FlatId));
    }
}
