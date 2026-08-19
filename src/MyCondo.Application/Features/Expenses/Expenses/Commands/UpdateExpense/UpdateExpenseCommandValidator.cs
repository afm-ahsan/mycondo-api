using FluentValidation;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.UpdateExpense;

public sealed class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.ExpenseTypeId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Payee).MaximumLength(200);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.PaymentMethod).Must(BeAValidPaymentMethod)
            .WithMessage($"PaymentMethod must be one of: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.");
    }

    private static bool BeAValidPaymentMethod(string value) => Enum.TryParse<PaymentMethod>(value, out _);
}
