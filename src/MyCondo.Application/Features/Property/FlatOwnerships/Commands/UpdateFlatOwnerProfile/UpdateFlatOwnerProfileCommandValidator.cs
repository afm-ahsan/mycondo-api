using FluentValidation;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.UpdateFlatOwnerProfile;

public sealed class UpdateFlatOwnerProfileCommandValidator : AbstractValidator<UpdateFlatOwnerProfileCommand>
{
    public UpdateFlatOwnerProfileCommandValidator()
    {
        RuleFor(x => x.ResidentId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AlternatePhone).MaximumLength(20);
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
        RuleFor(x => x.EmergencyContactPhone).MaximumLength(20);
    }
}
