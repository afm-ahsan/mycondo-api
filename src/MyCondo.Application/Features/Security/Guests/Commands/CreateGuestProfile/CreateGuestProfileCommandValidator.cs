using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Security.Guests.Commands.CreateGuestProfile;

public sealed class CreateGuestProfileCommandValidator : AbstractValidator<CreateGuestProfileCommand>
{
    public CreateGuestProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.IdentityDocumentType).MaximumLength(40);
        RuleFor(x => x.IdentityDocumentNumber).MaximumLength(60);
    }
}
