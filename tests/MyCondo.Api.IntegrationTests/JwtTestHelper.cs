using Microsoft.IdentityModel.JsonWebTokens;

namespace MyCondo.Api.IntegrationTests;

/// <summary>Decodes a JWT's claims for test assertions, without re-validating its signature — the
/// signature/audience/scheme validation itself is exercised by the actual HTTP request pipeline in
/// these tests; this only inspects what ended up inside an already-issued token.</summary>
public static class JwtTestHelper
{
    public static JwtClaims Decode(string accessToken) => new(new JsonWebTokenHandler().ReadJsonWebToken(accessToken));
}

public sealed class JwtClaims(JsonWebToken token)
{
    public bool ContainsClaim(string type) => token.TryGetClaim(type, out _);

    public string? GetClaimValue(string type) => token.TryGetClaim(type, out System.Security.Claims.Claim claim) ? claim.Value : null;

    /// <summary>Every value of a claim type that can appear more than once in the same token — e.g.
    /// "perm"/"bperm"/"building_ids" (see JwtTokenService.WriteAccessToken), where each grant is its
    /// own repeated claim, not a single delimited/array value.</summary>
    public List<string> GetClaimValues(string type) =>
        token.Claims.Where(c => c.Type == type).Select(c => c.Value).ToList();

    public string GetAudience() => token.Audiences.Single();
}
