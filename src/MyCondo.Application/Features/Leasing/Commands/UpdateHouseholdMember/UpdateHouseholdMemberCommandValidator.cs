using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Leasing.Commands.UpdateHouseholdMember;

public sealed class UpdateHouseholdMemberCommandValidator : AbstractValidator<UpdateHouseholdMemberCommand>
{
    public UpdateHouseholdMemberCommandValidator()
    {
        RuleFor(x => x.HouseholdMemberId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RelationshipToPrimary).NotEmpty().MaximumLength(50)
            .Must(r => HouseholdRelationships.Allowed.Contains(r))
            .WithMessage("Relationship must be one of: Father, Mother, Spouse, Child.");
        RuleFor(x => x.DateOfBirth).NotEmpty()
            .Must(dob => dob is null || dob.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.Phone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.NationalIdNumber).MaximumLength(50);
        RuleFor(x => x.Gender).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BirthCertificateNumber).MaximumLength(50);
        RuleFor(x => x.BloodGroup).MaximumLength(10);
        RuleFor(x => x.Religion).MaximumLength(50);
        RuleFor(x => x.Nationality).MaximumLength(50);
        RuleFor(x => x.Occupation).MaximumLength(200);
        RuleFor(x => x)
            .Must(x => !string.Equals(x.RelationshipToPrimary, "Child", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(x.NationalIdNumber) || !string.IsNullOrWhiteSpace(x.BirthCertificateNumber))
            .WithMessage("A Child household member requires either a National ID or a Birth Certificate number.");
    }
}
