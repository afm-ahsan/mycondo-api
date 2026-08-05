using FluentValidation;
using MyCondo.Domain.Features.Security.Common;

namespace MyCondo.Application.Features.Security.ServiceProviders.Commands.SetServiceProviderStatus;

public sealed class SetServiceProviderStatusCommandValidator : AbstractValidator<SetServiceProviderStatusCommand>
{
    public SetServiceProviderStatusCommandValidator()
    {
        RuleFor(x => x.ServiceProviderProfileId).NotEmpty();
        RuleFor(x => x.Status).Must(BeAValidStatus)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<RecurringAccessProfileStatus>())}.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(400)
            .When(x => string.Equals(x.Status, nameof(RecurringAccessProfileStatus.Suspended), StringComparison.Ordinal)
                       || string.Equals(x.Status, nameof(RecurringAccessProfileStatus.Blocked), StringComparison.Ordinal));
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<RecurringAccessProfileStatus>(value, out _);
}
