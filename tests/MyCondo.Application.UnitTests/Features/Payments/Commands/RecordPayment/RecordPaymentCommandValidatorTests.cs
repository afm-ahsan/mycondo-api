using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payments.Commands.RecordPayment;

namespace MyCondo.Application.UnitTests.Features.Payments.Commands.RecordPayment;

public class RecordPaymentCommandValidatorTests
{
    private readonly RecordPaymentCommandValidator _validator = new();
    private static readonly DateOnly BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Valid_Command_Passes()
    {
        RecordPaymentCommand command = new(Guid.NewGuid(), 500m, "Cash", "REF-1", BusinessDate, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FlatId_Fails()
    {
        RecordPaymentCommand command = new(Guid.Empty, 500m, "Cash", null, BusinessDate, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPaymentCommand.FlatId));
    }

    [Fact]
    public void Zero_Amount_Fails()
    {
        RecordPaymentCommand command = new(Guid.NewGuid(), 0m, "Cash", null, BusinessDate, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPaymentCommand.Amount));
    }

    [Fact]
    public void Invalid_PaymentMethod_Fails()
    {
        RecordPaymentCommand command = new(Guid.NewGuid(), 500m, "Crypto", null, BusinessDate, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPaymentCommand.PaymentMethod));
    }
}
