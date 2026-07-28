using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUserContextResolver userContextResolver,
    IRequestIpAccessor ipAccessor,
    IClock clock,
    ILogger<LoginCommandHandler> logger
) : IRequestHandler<LoginCommand, AuthTokensDto>
{
    public async ValueTask<AuthTokensDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        string normalizedEmail = command.Email.Trim().ToLowerInvariant();
        User? user = await users.GetByEmailAsync(command.TenantId, normalizedEmail, cancellationToken);

        if (user is null
            || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            // Identical message for unknown email and wrong password — no enumeration leak.
            logger.LogInformation(
                "Failed login attempt for {Email} on tenant {TenantId}",
                normalizedEmail, command.TenantId);
            throw new ForbiddenException("Invalid email or password.");
        }

        if (user.Status != UserStatus.Active)
        {
            logger.LogInformation(
                "Login blocked: user {UserId} status {Status}",
                user.Id, user.Status);
            throw new ForbiddenException("Account is not active.");
        }

        string ip = ipAccessor.IpAddress;
        user.RecordLogin(ip, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        AuthenticatedUserDto auth = await userContextResolver.ResolveAsync(user, cancellationToken);
        AuthTokensDto tokens = await tokenService.IssueAsync(auth, ip, cancellationToken);

        logger.LogInformation("User {UserId} logged in", user.Id);
        return tokens;
    }
}
