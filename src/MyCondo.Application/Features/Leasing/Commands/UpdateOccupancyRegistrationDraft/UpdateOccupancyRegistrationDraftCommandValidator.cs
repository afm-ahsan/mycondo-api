using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Leasing.Commands.UpdateOccupancyRegistrationDraft;

public sealed class UpdateOccupancyRegistrationDraftCommandValidator
    : AbstractValidator<UpdateOccupancyRegistrationDraftCommand>
{
    public UpdateOccupancyRegistrationDraftCommandValidator()
    {
        RuleFor(x => x.OccupancyRegistrationId).NotEmpty();
        RuleFor(x => x.PrimaryFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrimaryPhone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.PrimaryEmail).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.PrimaryEmail));
        RuleFor(x => x.PrimaryNationalIdNumber).MaximumLength(50);
        RuleFor(x => x.PrimaryPermanentAddress).MaximumLength(500);
        RuleFor(x => x.EmergencyContactPhone).MustBeValidBangladeshMobileNumber();
    }
}
