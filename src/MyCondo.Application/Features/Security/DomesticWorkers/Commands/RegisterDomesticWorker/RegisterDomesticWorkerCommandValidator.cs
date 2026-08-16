using FluentValidation;
using MyCondo.Application.Common.Validation;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Commands.RegisterDomesticWorker;

public sealed class RegisterDomesticWorkerCommandValidator : AbstractValidator<RegisterDomesticWorkerCommand>
{
    public RegisterDomesticWorkerCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.IdentityDocumentType).MaximumLength(40);
        RuleFor(x => x.IdentityDocumentNumber).MaximumLength(60);
        RuleFor(x => x.EmergencyContactName).MaximumLength(200);
        RuleFor(x => x.EmergencyContactPhone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.WorkerType).Must(BeAValidWorkerType)
            .WithMessage($"WorkerType must be one of: {string.Join(", ", Enum.GetNames<DomesticWorkerType>())}.");
    }

    private static bool BeAValidWorkerType(string value) => Enum.TryParse<DomesticWorkerType>(value, out _);
}
