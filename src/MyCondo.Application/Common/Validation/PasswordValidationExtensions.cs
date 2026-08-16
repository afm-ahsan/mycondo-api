using System.Text.RegularExpressions;
using FluentValidation;

namespace MyCondo.Application.Common.Validation;

/// <summary>
/// Reusable FluentValidation rule for every command that sets a password (ChangePassword, CreateUser,
/// RegisterUser, ProvisionOrganizationWithAdmin) — avoids duplicating the strength regex across
/// validators. Enforces the MyCondo MVP-1 password policy: 6-128 characters, at least one uppercase
/// letter, one lowercase letter, one digit, and one special character.
/// </summary>
public static partial class PasswordValidationExtensions
{
    public const int MinimumLength = 6;
    public const int MaximumLength = 128;

    public static IRuleBuilderOptions<T, string> MustBeAStrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .MinimumLength(MinimumLength).WithMessage($"Password must be at least {MinimumLength} characters.")
            .MaximumLength(MaximumLength)
            .Matches(UppercaseLetter()).WithMessage("Password must contain an uppercase letter.")
            .Matches(LowercaseLetter()).WithMessage("Password must contain a lowercase letter.")
            .Matches(Digit()).WithMessage("Password must contain a digit.")
            .Matches(SpecialCharacter()).WithMessage("Password must contain a special character (e.g. !@#$%).");

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseLetter();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercaseLetter();

    [GeneratedRegex(@"\d")]
    private static partial Regex Digit();

    [GeneratedRegex(@"[^A-Za-z0-9\s]")]
    private static partial Regex SpecialCharacter();
}
