using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.RegisterFlatOwner;

public sealed class RegisterFlatOwnerCommandValidator : AbstractValidator<RegisterFlatOwnerCommand>
{
    public RegisterFlatOwnerCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AlternatePhone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.NationalIdNumber).MaximumLength(50);
        RuleFor(x => x.PassportNumber).MaximumLength(50);
        RuleFor(x => x.Gender).MaximumLength(20);
        RuleFor(x => x.PresentAddress).MaximumLength(400);
        RuleFor(x => x.PermanentAddress).MaximumLength(400);
        RuleFor(x => x.FatherName).MaximumLength(200);
        RuleFor(x => x.MotherName).MaximumLength(200);
        RuleFor(x => x.MaritalStatus).MaximumLength(20);
        RuleFor(x => x.Profession).MaximumLength(200);
        RuleFor(x => x.Employer).MaximumLength(200);
        RuleFor(x => x.OfficeAddress).MaximumLength(400);
        RuleFor(x => x.EmergencyContactName).MaximumLength(200);
        RuleFor(x => x.EmergencyContactPhone).MustBeValidBangladeshMobileNumber();
    }
}
