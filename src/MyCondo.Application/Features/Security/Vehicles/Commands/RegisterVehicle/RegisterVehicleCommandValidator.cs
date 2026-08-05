using FluentValidation;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.RegisterVehicle;

public sealed class RegisterVehicleCommandValidator : AbstractValidator<RegisterVehicleCommand>
{
    public RegisterVehicleCommandValidator()
    {
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Make).MaximumLength(60);
        RuleFor(x => x.Model).MaximumLength(60);
        RuleFor(x => x.Color).MaximumLength(30);
        RuleFor(x => x.VehicleType).Must(BeAValidVehicleType)
            .WithMessage($"VehicleType must be one of: {string.Join(", ", Enum.GetNames<VehicleType>())}.");
        RuleFor(x => x.OwnershipCategory).Must(BeAValidOwnershipCategory)
            .WithMessage($"OwnershipCategory must be one of: {string.Join(", ", Enum.GetNames<VehicleOwnershipCategory>())}.");
    }

    private static bool BeAValidVehicleType(string value) => Enum.TryParse<VehicleType>(value, out _);

    private static bool BeAValidOwnershipCategory(string value) => Enum.TryParse<VehicleOwnershipCategory>(value, out _);
}
