using FluentValidation;
using MyCondo.Domain.Features.Security.Common;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Commands.SetDomesticWorkerStatus;

public sealed class SetDomesticWorkerStatusCommandValidator : AbstractValidator<SetDomesticWorkerStatusCommand>
{
    public SetDomesticWorkerStatusCommandValidator()
    {
        RuleFor(x => x.DomesticWorkerProfileId).NotEmpty();
        RuleFor(x => x.Status).Must(BeAValidStatus)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<RecurringAccessProfileStatus>())}.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(400)
            .When(x => string.Equals(x.Status, nameof(RecurringAccessProfileStatus.Suspended), StringComparison.Ordinal)
                       || string.Equals(x.Status, nameof(RecurringAccessProfileStatus.Blocked), StringComparison.Ordinal));
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<RecurringAccessProfileStatus>(value, out _);
}
