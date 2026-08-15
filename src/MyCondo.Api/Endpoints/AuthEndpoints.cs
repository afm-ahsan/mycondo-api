using Mediator;
using MyCondo.Api.Authentication;
using MyCondo.Application.Features.Auth.Commands.ChangePassword;
using MyCondo.Application.Features.Auth.Commands.Login;
using MyCondo.Application.Features.Auth.Commands.Logout;
using MyCondo.Application.Features.Auth.Commands.RefreshToken;
using MyCondo.Application.Features.Auth.Commands.Register;
using MyCondo.Application.Features.Auth.Commands.RemoveMyAvatar;
using MyCondo.Application.Features.Auth.Commands.UpdateMyProfile;
using MyCondo.Application.Features.Auth.Commands.UploadMyAvatar;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Auth.Queries.GetMyAvatar;
using MyCondo.Application.Features.Auth.Queries.GetMyProfile;

namespace MyCondo.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        // Register/Login/Refresh are anonymous — there's no JWT yet, so TenantContextAccessor has no
        // claim to read for RLS purposes. Each declares its own TenantId in the request body, so we
        // stash it on HttpContext.Items before dispatching; TenantContextAccessor falls back to it.
        // See TenantContextAccessor's doc comment and mycondo-docs ADR-013.
        group.MapPost("/register", async (RegisterUserCommand command, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                http.Items[TenantContextAccessor.RequestedTenantItemKey] = command.TenantId;
                AuthTokensDto result = await sender.Send(command, ct);
                RefreshTokenCookie.Append(http, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(AuthResponse.From(result));
            })
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<AuthResponse>(StatusCodes.Status200OK);

        group.MapPost("/login", async (LoginCommand command, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                http.Items[TenantContextAccessor.RequestedTenantItemKey] = command.TenantId;
                AuthTokensDto result = await sender.Send(command, ct);
                RefreshTokenCookie.Append(http, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(AuthResponse.From(result));
            })
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<AuthResponse>(StatusCodes.Status200OK);

        // The refresh token itself comes from the HttpOnly cookie, not the body — RefreshRequest only
        // carries TenantId (still needed pre-auth for RLS, same reasoning as Register/Login above).
        group.MapPost("/refresh", async (RefreshRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                http.Items[TenantContextAccessor.RequestedTenantItemKey] = request.TenantId;
                string token = RefreshTokenCookie.Read(http) ?? string.Empty;
                AuthTokensDto result = await sender.Send(new RefreshTokenCommand(request.TenantId, token), ct);
                RefreshTokenCookie.Append(http, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(AuthResponse.From(result));
            })
            .AllowAnonymous()
            .Produces<AuthResponse>(StatusCodes.Status200OK);

        group.MapPost("/logout", async (ISender sender, HttpContext http, CancellationToken ct) =>
            {
                string token = RefreshTokenCookie.Read(http) ?? string.Empty;
                await sender.Send(new LogoutCommand(token), ct);
                RefreshTokenCookie.Clear(http);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/change-password", async (ChangePasswordCommand command, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(command, ct);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
            {
                UserProfileDto profile = await sender.Send(new GetMyProfileQuery(), ct);
                return Results.Ok(profile);
            })
            .RequireAuthorization()
            .Produces<UserProfileDto>(StatusCodes.Status200OK);

        group.MapPut("/me", async (UpdateMyProfileCommand command, ISender sender, CancellationToken ct) =>
            {
                UserProfileDto profile = await sender.Send(command, ct);
                return Results.Ok(profile);
            })
            .RequireAuthorization()
            .Produces<UserProfileDto>(StatusCodes.Status200OK);

        group.MapPost("/me/avatar", async (IFormFile file, ISender sender, CancellationToken ct) =>
            {
                await using Stream stream = file.OpenReadStream();
                UserProfileDto profile = await sender.Send(
                    new UploadMyAvatarCommand(stream, file.FileName, file.ContentType, file.Length), ct);
                return Results.Ok(profile);
            })
            .RequireAuthorization()
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UserProfileDto>(StatusCodes.Status200OK);

        group.MapDelete("/me/avatar", async (ISender sender, CancellationToken ct) =>
            {
                UserProfileDto profile = await sender.Send(new RemoveMyAvatarCommand(), ct);
                return Results.Ok(profile);
            })
            .RequireAuthorization()
            .Produces<UserProfileDto>(StatusCodes.Status200OK);

        group.MapGet("/me/avatar", async (ISender sender, CancellationToken ct) =>
            {
                AvatarContentDto? avatar = await sender.Send(new GetMyAvatarQuery(), ct);
                return avatar is null ? Results.NotFound() : Results.File(avatar.Content, avatar.ContentType);
            })
            .RequireAuthorization();

        return app;
    }
}

/// <summary>
/// The public wire shape of Register/Login/Refresh — everything AuthTokensDto carries except the
/// refresh token, which never leaves the server as a JSON field (see RefreshTokenCookie).
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    AuthenticatedUserDto User)
{
    public static AuthResponse From(AuthTokensDto tokens) =>
        new(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.User);
}

public sealed record RefreshRequest(Guid TenantId);
