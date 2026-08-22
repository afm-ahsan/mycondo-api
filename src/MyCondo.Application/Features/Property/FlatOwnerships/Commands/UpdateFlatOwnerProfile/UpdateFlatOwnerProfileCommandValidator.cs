using FluentValidation;
using MyCondo.Application.Common.Validation;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Commands.UpdateFlatOwnerProfile;

public sealed class UpdateFlatOwnerProfileCommandValidator : AbstractValidator<UpdateFlatOwnerProfileCommand>
{
    public UpdateFlatOwnerProfileCommandValidator(IResidentRepository residents)
    {
        RuleFor(x => x.ResidentId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AlternatePhone).MustBeValidBangladeshMobileNumber();
        // National ID is masked on read and never round-tripped through the edit form, so a blank
        // value here conventionally means "keep the existing value" (see UpdateOwnerDetails) — but
        // only when the resident already has one on file. Otherwise this would let an owner's full
        // profile be saved without ever supplying a National ID, so a blank value is only accepted
        // when the resident record we're updating already has a non-blank one.
        RuleFor(x => x.NationalIdNumber)
            .MustAsync(async (command, nationalIdNumber, cancellationToken) =>
            {
                if (!string.IsNullOrWhiteSpace(nationalIdNumber))
                {
                    return true;
                }

                Resident? resident = await residents.GetByIdAsync(new ResidentId(command.ResidentId), cancellationToken);
                return resident is not null && !string.IsNullOrWhiteSpace(resident.NationalIdNumber);
            })
            .WithMessage("National ID is required.")
            .MaximumLength(50);
        RuleFor(x => x.PassportNumber).MaximumLength(50);
        RuleFor(x => x.Gender).MaximumLength(20);
        RuleFor(x => x.DateOfBirth)
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
