using FluentValidation;
using MyCondo.Application.Common.Validation;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Application.Features.Payroll.StaffMembers.Commands.RegisterStaffMember;

public sealed class RegisterStaffMemberCommandValidator : AbstractValidator<RegisterStaffMemberCommand>
{
    public RegisterStaffMemberCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.Role).Must(BeAValidRole)
            .WithMessage($"Role must be one of: {string.Join(", ", Enum.GetNames<StaffRole>())}.");
    }

    private static bool BeAValidRole(string value) => Enum.TryParse<StaffRole>(value, out _);
}
