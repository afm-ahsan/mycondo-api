using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Billing.Commands.VoidInvoice;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.VoidInvoice;

public class VoidInvoiceCommandValidatorTests
{
    private readonly VoidInvoiceCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        VoidInvoiceCommand command = new(Guid.NewGuid(), "Issued in error");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InvoiceId_Fails()
    {
        VoidInvoiceCommand command = new(Guid.Empty, "Issued in error");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(VoidInvoiceCommand.InvoiceId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Blank_Reason_Fails(string reason)
    {
        VoidInvoiceCommand command = new(Guid.NewGuid(), reason);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(VoidInvoiceCommand.Reason));
    }
}
