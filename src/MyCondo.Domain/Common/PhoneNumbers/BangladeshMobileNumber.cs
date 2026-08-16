using System.Text.RegularExpressions;

namespace MyCondo.Domain.Common.PhoneNumbers;

/// <summary>
/// Canonicalizes Bangladesh mobile numbers to <c>+8801XXXXXXXXX</c> — the single format persisted
/// anywhere a phone/mobile field exists (Residents, Users, HouseholdMembers, OccupancyRegistrations,
/// GuestProfiles, DomesticWorkerProfiles, ServiceProviderProfiles, StaffMembers, GasCylinderSuppliers,
/// SebaVisitDetails). Accepts the common variants users type or paste (with/without country code,
/// domestic trunk prefix, spaces, hyphens, parentheses) so every entry point normalizes to the same
/// value before persistence — this is what makes exact-string equality (e.g. GuestProfile's
/// tenant+phone uniqueness index) actually work across differently-formatted input.
/// </summary>
public static partial class BangladeshMobileNumber
{
    /// <summary>Null/empty input is a valid "no phone provided" — <paramref name="normalized"/> is
    /// null and this returns true. A non-empty input that isn't a valid BD mobile number returns
    /// false; formatting characters (spaces/hyphens/parentheses) are stripped first, but any other
    /// non-digit content (letters, etc.) is treated as invalid rather than silently discarded.</summary>
    public static bool TryNormalize(string? raw, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        string cleaned = FormattingCharactersRegex().Replace(raw.Trim(), string.Empty);
        if (!DigitsWithOptionalLeadingPlusRegex().IsMatch(cleaned))
        {
            return false;
        }

        string local;
        if (cleaned.StartsWith("+880", StringComparison.Ordinal))
        {
            local = cleaned[4..];
        }
        else if (cleaned.StartsWith("880", StringComparison.Ordinal) && cleaned.Length > 10)
        {
            local = cleaned[3..];
        }
        else if (cleaned.StartsWith('+'))
        {
            return false; // some other country code — not a BD number
        }
        else if (cleaned.StartsWith('0'))
        {
            local = cleaned[1..];
        }
        else
        {
            local = cleaned;
        }

        if (!LocalMobileNumberRegex().IsMatch(local))
        {
            return false;
        }

        normalized = "+880" + local;
        return true;
    }

    /// <summary>Throws <see cref="FormatException"/> for a non-empty, invalid value. Callers that
    /// already validated via <see cref="TryNormalize"/> (e.g. FluentValidation) should not hit the
    /// throw path in practice; entities call this directly as the single normalization point.</summary>
    public static string? Normalize(string? raw)
    {
        if (!TryNormalize(raw, out string? normalized))
        {
            throw new FormatException($"'{raw}' is not a valid Bangladesh mobile number.");
        }

        return normalized;
    }

    /// <summary>True only for an already-canonical <c>+8801XXXXXXXXX</c> value.</summary>
    public static bool IsValidCanonical(string value) => CanonicalRegex().IsMatch(value);

    [GeneratedRegex(@"[\s\-()]")]
    private static partial Regex FormattingCharactersRegex();

    [GeneratedRegex(@"^\+?[0-9]+$")]
    private static partial Regex DigitsWithOptionalLeadingPlusRegex();

    [GeneratedRegex(@"^1[3-9][0-9]{8}$")]
    private static partial Regex LocalMobileNumberRegex();

    [GeneratedRegex(@"^\+8801[3-9][0-9]{8}$")]
    private static partial Regex CanonicalRegex();
}
