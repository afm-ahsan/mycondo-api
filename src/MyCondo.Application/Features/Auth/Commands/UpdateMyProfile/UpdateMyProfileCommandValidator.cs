using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Auth.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).MustBeValidBangladeshMobileNumber();
    }
}
