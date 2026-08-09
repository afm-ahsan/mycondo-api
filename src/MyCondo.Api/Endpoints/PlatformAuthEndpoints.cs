using Mediator;
using MyCondo.Api.Authentication;
using MyCondo.Application.Features.Platform.Commands.PlatformLogin;
using MyCondo.Application.Features.Platform.Commands.RefreshPlatformToken;
using MyCondo.Application.Features.Platform.Commands.RevokePlatformToken;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Platform-scope analogue of <see cref="AuthEndpoints"/>. Deliberately a separate route group with
/// no shared request/response types with the tenant auth endpoints — <see cref="PlatformLoginCommand"/>
/// structurally has no TenantId field, so there is nothing here for the backend to "guess" about
/// which identity type a request belongs to. See mycondo-docs ADR-019.
/// </summary>
public static class PlatformAuthEndpoints
{
    public static IEndpointRouteBuilder MapPlatformAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/platform/auth").WithTags("Platform Auth");

        group.MapPost("/login", async (PlatformLoginCommand command, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                PlatformAuthTokensDto result = await sender.Send(command, ct);
                PlatformRefreshTokenCookie.Append(http, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(PlatformAuthResponse.From(result));
            })
            .AllowAnonymous()
            .RequireRateLimiting("platform-auth")
            .Produces<PlatformAuthResponse>(StatusCodes.Status200OK);

        // Deliberately NOT rate-limited by "platform-auth" — same reasoning as the tenant side's
        // /api/v1/auth/refresh (see AuthEndpoints.cs / RefreshRateLimitingTests): this is gated by
        // possession of the mycondo_platform_rt HttpOnly cookie, not a guessable credential, so the
        // credential-guessing-oriented limit doesn't apply. Fixed during Phase 1 live PostgreSQL
        // verification (see mycondo-phase1-final-postgresql-rls-verification-prompt.md) — it was
        // originally, incorrectly, applied here, deviating from the established tenant pattern.
        group.MapPost("/refresh", async (ISender sender, HttpContext http, CancellationToken ct) =>
            {
                string token = PlatformRefreshTokenCookie.Read(http) ?? string.Empty;
                PlatformAuthTokensDto result = await sender.Send(new RefreshPlatformTokenCommand(token), ct);
                PlatformRefreshTokenCookie.Append(http, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(PlatformAuthResponse.From(result));
            })
            .AllowAnonymous()
            .Produces<PlatformAuthResponse>(StatusCodes.Status200OK);

        group.MapPost("/logout", async (ISender sender, HttpContext http, CancellationToken ct) =>
            {
                string token = PlatformRefreshTokenCookie.Read(http) ?? string.Empty;
                await sender.Send(new RevokePlatformTokenCommand(token), ct);
                PlatformRefreshTokenCookie.Clear(http);
                return Results.NoContent();
            })
            .RequireAuthorization(PlatformAuthenticationDefaults.AuthorizationPolicyName)
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record PlatformAuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    PlatformAuthenticatedUserDto User)
{
    public static PlatformAuthResponse From(PlatformAuthTokensDto tokens) =>
        new(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.User);
}
