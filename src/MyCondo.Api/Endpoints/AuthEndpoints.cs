using Mediator;
using MyCondo.Api.Authentication;
using MyCondo.Application.Features.Auth.Commands.ChangePassword;
using MyCondo.Application.Features.Auth.Commands.Login;
using MyCondo.Application.Features.Auth.Commands.Logout;
using MyCondo.Application.Features.Auth.Commands.RefreshToken;
using MyCondo.Application.Features.Auth.Commands.Register;
using MyCondo.Application.Features.Auth.DTOs;
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
                return Results.Ok(result);
            })
            .AllowAnonymous();

        group.MapPost("/login", async (LoginCommand command, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                http.Items[TenantContextAccessor.RequestedTenantItemKey] = command.TenantId;
                AuthTokensDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                http.Items[TenantContextAccessor.RequestedTenantItemKey] = command.TenantId;
                AuthTokensDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .AllowAnonymous();

        group.MapPost("/logout", async (LogoutCommand command, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(command, ct);
                return Results.NoContent();
            })
            .RequireAuthorization();

        group.MapPost("/change-password", async (ChangePasswordCommand command, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(command, ct);
                return Results.NoContent();
            })
            .RequireAuthorization();

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
            {
                UserProfileDto profile = await sender.Send(new GetMyProfileQuery(), ct);
                return Results.Ok(profile);
            })
            .RequireAuthorization();

        return app;
    }
}
