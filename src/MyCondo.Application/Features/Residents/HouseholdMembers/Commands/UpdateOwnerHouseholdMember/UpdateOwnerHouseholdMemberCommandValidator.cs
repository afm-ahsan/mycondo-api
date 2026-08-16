using FluentValidation;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Application.Features.Residents.HouseholdMembers.Commands.UpdateOwnerHouseholdMember;

public sealed class UpdateOwnerHouseholdMemberCommandValidator : AbstractValidator<UpdateOwnerHouseholdMemberCommand>
{
    public UpdateOwnerHouseholdMemberCommandValidator()
    {
        RuleFor(x => x.ResidentHouseholdMemberId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RelationshipType).NotEmpty()
            .Must(r => Enum.TryParse<RelationshipType>(r, out _))
            .WithMessage($"RelationshipType must be one of: {string.Join(", ", Enum.GetNames<RelationshipType>())}.");
        RuleFor(x => x.Gender).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).NotEmpty()
            .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x.NationalIdNumber).MaximumLength(50);
        RuleFor(x => x.BirthCertificateNumber).MaximumLength(50);
        RuleFor(x => x.BloodGroup).MaximumLength(10);
        RuleFor(x => x.Religion).MaximumLength(50);
        RuleFor(x => x.Nationality).MaximumLength(50);
        RuleFor(x => x.Occupation).MaximumLength(200);
        RuleFor(x => x)
            .Must(x => !string.Equals(x.RelationshipType, nameof(RelationshipType.Child), StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(x.NationalIdNumber) || !string.IsNullOrWhiteSpace(x.BirthCertificateNumber))
            .WithMessage("A Child household member requires either a National ID or a Birth Certificate number.");
    }
}
