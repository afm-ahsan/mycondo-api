using FluentValidation;
using MyCondo.Domain.Common.PhoneNumbers;

namespace MyCondo.Application.Common.Validation;

/// <summary>
/// Reusable FluentValidation rule for every command carrying a Bangladesh mobile/phone field —
/// avoids duplicating the format check across CreateResident, UpdateResident, RegisterFlatOwner,
/// RegisterUser, CreateUser, CreateGuestProfile, etc. Null/empty passes (pair with <c>NotEmpty()</c>
/// separately for fields that are required); a non-empty value must normalize via
/// <see cref="BangladeshMobileNumber.TryNormalize"/>.
/// </summary>
public static class BangladeshMobileNumberValidationExtensions
{
    public static IRuleBuilderOptions<T, string?> MustBeValidBangladeshMobileNumber<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(value => BangladeshMobileNumber.TryNormalize(value, out _))
            .WithMessage("Enter a valid Bangladesh mobile number.");
}
