using System.ComponentModel.DataAnnotations;

namespace MyCondo.Application.Common.Settings;

public sealed record JwtSettings
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = default!;
    [Required] public string Audience { get; init; } = default!;

    /// <summary>
    /// The audience Platform-scope tokens are issued/validated against — deliberately distinct from
    /// <see cref="Audience"/> (the tenant audience), so a token minted for one identity type is
    /// rejected outright by the other's JWT bearer scheme before any permission/RLS check even runs.
    /// See mycondo-docs ADR-019.
    /// </summary>
    [Required] public string PlatformAudience { get; init; } = default!;

    [Required, MinLength(32)] public string SigningKey { get; init; } = default!;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDays { get; init; } = 7;
}
