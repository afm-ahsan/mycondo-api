using FluentValidation;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Application.Features.Security.ServiceProviders.Commands.RegisterServiceProvider;

public sealed class RegisterServiceProviderCommandValidator : AbstractValidator<RegisterServiceProviderCommand>
{
    public RegisterServiceProviderCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ServiceDescription).MaximumLength(200);
        RuleFor(x => x.IdentityDocumentType).MaximumLength(40);
        RuleFor(x => x.IdentityDocumentNumber).MaximumLength(60);
        RuleFor(x => x.ProviderType).Must(BeAValidProviderType)
            .WithMessage($"ProviderType must be one of: {string.Join(", ", Enum.GetNames<ServiceProviderType>())}.");
    }

    private static bool BeAValidProviderType(string value) => Enum.TryParse<ServiceProviderType>(value, out _);
}
