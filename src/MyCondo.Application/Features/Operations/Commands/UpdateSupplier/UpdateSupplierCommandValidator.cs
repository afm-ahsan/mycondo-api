using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.UpdateSupplier;

public sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.GasCylinderSupplierId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPhone).MaximumLength(30);
        RuleFor(x => x.ContactEmail).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
