using System.ComponentModel.DataAnnotations;

namespace MyCondo.Application.Common.Settings;

public sealed record JwtSettings
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = default!;
    [Required] public string Audience { get; init; } = default!;
    [Required, MinLength(32)] public string SigningKey { get; init; } = default!;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDays { get; init; } = 7;
}
