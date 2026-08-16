using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Residents.Commands.UpdateResident;

public sealed class UpdateResidentCommandValidator : AbstractValidator<UpdateResidentCommand>
{
    public UpdateResidentCommandValidator()
    {
        RuleFor(x => x.ResidentId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
