using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Expenses.Expenses.Commands.CreateExpense;

namespace MyCondo.Application.UnitTests.Features.Expenses.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandValidatorTests
{
    private readonly CreateExpenseCommandValidator _validator = new();

    private static CreateExpenseCommand ValidCommand(decimal amount = 100m, string paymentMethod = "Cash") => new(
        Guid.NewGuid(), Guid.NewGuid(), null, new DateOnly(2026, 8, 1), null, "Cleaning", null, null, amount, false,
        paymentMethod, null);

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Non_Positive_Amount_Fails(decimal amount)
    {
        ValidationResult result = _validator.Validate(ValidCommand(amount: amount));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExpenseCommand.Amount));
    }

    [Fact]
    public void Invalid_PaymentMethod_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(paymentMethod: "Bitcoin"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExpenseCommand.PaymentMethod));
    }

    [Fact]
    public void Empty_Description_Fails()
    {
        CreateExpenseCommand command = ValidCommand() with { Description = "" };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExpenseCommand.Description));
    }
}
