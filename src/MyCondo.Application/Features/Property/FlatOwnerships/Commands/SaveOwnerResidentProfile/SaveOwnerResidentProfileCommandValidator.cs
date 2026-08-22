using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.SaveOwnerResidentProfile;

public sealed class SaveOwnerResidentProfileCommandValidator : AbstractValidator<SaveOwnerResidentProfileCommand>
{
    public SaveOwnerResidentProfileCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AlternatePhone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.NationalIdNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PassportNumber).MaximumLength(50);
        RuleFor(x => x.Gender).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).NotEmpty()
            .Must(dob => dob is null || dob.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.PresentAddress).NotEmpty().MaximumLength(400);
        RuleFor(x => x.PermanentAddress).NotEmpty().MaximumLength(400);
        RuleFor(x => x.FatherName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MotherName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MaritalStatus).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Profession).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Employer).MaximumLength(200);
        RuleFor(x => x.OfficeAddress).MaximumLength(400);
        RuleFor(x => x.EmergencyContactName).MaximumLength(200);
        RuleFor(x => x.EmergencyContactPhone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.BloodGroup).MaximumLength(10);
        RuleFor(x => x.Religion).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Nationality).NotEmpty().MaximumLength(50);
    }
}
