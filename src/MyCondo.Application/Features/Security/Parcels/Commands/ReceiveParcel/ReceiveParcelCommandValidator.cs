using FluentValidation;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Commands.ReceiveParcel;

public sealed class ReceiveParcelCommandValidator : AbstractValidator<ReceiveParcelCommand>
{
    public ReceiveParcelCommandValidator()
    {
        RuleFor(x => x.RecipientFlatId).NotEmpty();
        RuleFor(x => x.ParcelReference).MaximumLength(80);
        RuleFor(x => x.CourierProvider).MaximumLength(120);
        RuleFor(x => x.TrackingNumber).MaximumLength(120);
        RuleFor(x => x.SenderName).MaximumLength(200);
        RuleFor(x => x.StorageLocation).MaximumLength(200);
        RuleFor(x => x.PackageCount).GreaterThan(0);
        RuleFor(x => x.ParcelType).Must(BeAValidParcelType)
            .WithMessage($"ParcelType must be one of: {string.Join(", ", Enum.GetNames<ParcelType>())}.");
    }

    private static bool BeAValidParcelType(string value) => Enum.TryParse<ParcelType>(value, out _);
}
