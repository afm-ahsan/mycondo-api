namespace MyCondo.Application.Common;

/// <summary>
/// Masks sensitive identity-document numbers for general API responses — per the register
/// digitization spec: "Do not expose full identity numbers unless permission explicitly allows it."
/// No endpoint in this slice exposes the unmasked value; a dedicated reveal-with-elevated-permission
/// capability is deferred (flagged as an open decision, not an oversight).
/// </summary>
public static class IdentityMasking
{
    public static string? Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= 4
            ? new string('*', value.Length)
            : new string('*', value.Length - 4) + value[^4..];
    }
}
