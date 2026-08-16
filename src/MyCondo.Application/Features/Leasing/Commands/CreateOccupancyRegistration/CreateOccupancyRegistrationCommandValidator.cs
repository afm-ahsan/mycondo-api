using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Leasing.Commands.CreateOccupancyRegistration;

public sealed class CreateOccupancyRegistrationCommandValidator : AbstractValidator<CreateOccupancyRegistrationCommand>
{
    public CreateOccupancyRegistrationCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.OccupancyType).NotEmpty().Must(t => t is "Owner" or "Occupant")
            .WithMessage("OccupancyType must be 'Owner' or 'Occupant'.");
        RuleFor(x => x.PrimaryFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrimaryPhone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.PrimaryEmail).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.PrimaryEmail));
        RuleFor(x => x.PrimaryNationalIdNumber).MaximumLength(50);
        RuleFor(x => x.PrimaryPermanentAddress).MaximumLength(500);
        RuleFor(x => x.EmergencyContactPhone).MustBeValidBangladeshMobileNumber();
    }
}
