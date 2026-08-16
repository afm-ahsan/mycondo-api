using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Leasing.Commands.AddHouseholdMember;

public sealed class AddHouseholdMemberCommandValidator : AbstractValidator<AddHouseholdMemberCommand>
{
    public AddHouseholdMemberCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RelationshipToPrimary).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Phone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.NationalIdNumber).MaximumLength(50);
    }
}
