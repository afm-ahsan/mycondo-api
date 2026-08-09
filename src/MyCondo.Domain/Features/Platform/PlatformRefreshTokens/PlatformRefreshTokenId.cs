namespace MyCondo.Domain.Features.Platform.PlatformRefreshTokens;

public readonly record struct PlatformRefreshTokenId(Guid Value)
{
    public static PlatformRefreshTokenId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PlatformRefreshTokenId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new PlatformRefreshTokenId(g)
            : throw new FormatException($"Invalid PlatformRefreshTokenId: '{s}'");
}
