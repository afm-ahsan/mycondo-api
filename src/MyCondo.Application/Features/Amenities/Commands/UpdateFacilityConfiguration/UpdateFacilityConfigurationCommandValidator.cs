using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.UpdateFacilityConfiguration;

public sealed class UpdateFacilityConfigurationCommandValidator : AbstractValidator<UpdateFacilityConfigurationCommand>
{
    public UpdateFacilityConfigurationCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.BookingChargeAmount).GreaterThanOrEqualTo(0).When(x => x.BookingChargeAmount is not null);
        RuleFor(x => x.DepositAmount).GreaterThanOrEqualTo(0).When(x => x.DepositAmount is not null);
        RuleFor(x => x.CancellationDeadlineHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CancellationDeductionPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.GuestFeeAmount).GreaterThanOrEqualTo(0).When(x => x.GuestFeeAmount is not null);
        RuleFor(x => x.MinimumAgeUnaccompanied).GreaterThanOrEqualTo(0).When(x => x.MinimumAgeUnaccompanied is not null);
    }
}
