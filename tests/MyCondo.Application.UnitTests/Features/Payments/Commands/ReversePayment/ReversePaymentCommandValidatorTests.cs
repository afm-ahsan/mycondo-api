using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payments.Commands.ReversePayment;

namespace MyCondo.Application.UnitTests.Features.Payments.Commands.ReversePayment;

public class ReversePaymentCommandValidatorTests
{
    private readonly ReversePaymentCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        ReversePaymentCommand command = new(Guid.NewGuid(), "Duplicate entry");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_PaymentId_Fails()
    {
        ReversePaymentCommand command = new(Guid.Empty, "Duplicate entry");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReversePaymentCommand.PaymentId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Blank_Reason_Fails(string reason)
    {
        ReversePaymentCommand command = new(Guid.NewGuid(), reason);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReversePaymentCommand.Reason));
    }
}
