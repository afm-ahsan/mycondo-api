using FluentValidation;
using MyCondo.Application.Common.Validation;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Residents.Commands.CreateResident;

public sealed class CreateResidentCommandValidator : AbstractValidator<CreateResidentCommand>
{
    public CreateResidentCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.ResidentType).Must(BeAValidResidentType)
            .WithMessage($"ResidentType must be one of: {string.Join(", ", Enum.GetNames<ResidentType>())}.");
    }

    private static bool BeAValidResidentType(string value) => Enum.TryParse<ResidentType>(value, out _);
}
