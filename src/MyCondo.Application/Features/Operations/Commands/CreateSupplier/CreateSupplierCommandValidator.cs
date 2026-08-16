using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Operations.Commands.CreateSupplier;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPhone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.ContactEmail).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
