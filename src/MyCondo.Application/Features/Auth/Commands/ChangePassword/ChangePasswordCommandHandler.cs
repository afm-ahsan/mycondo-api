using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RefreshTokens;
using MyCondo.Domain.Features.Identity.Users;
// Aliased: the sibling `Auth.Commands.RefreshToken` command-feature namespace shadows the unqualified
// `RefreshToken` domain-type name from this file's own namespace scope.
using RefreshTokenEntity = MyCondo.Domain.Features.Identity.RefreshTokens.RefreshToken;

namespace MyCondo.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUserProvider currentUser,
    IRequestIpAccessor ipAccessor,
    IClock clock,
    ILogger<ChangePasswordCommandHandler> logger
) : IRequestHandler<ChangePasswordCommand>
{
    public async ValueTask<Unit> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userIdValue)
        {
            throw new ForbiddenException("Authentication required.");
        }

        UserId userId = new(userIdValue);
        User? user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userIdValue);

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            logger.LogInformation(
                "Password-change rejected: current password mismatch for user {UserId}", userId);
            throw new ForbiddenException("Current password is incorrect.");
        }

        DateTimeOffset now = clock.UtcNow;
        string newHash = passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newHash, now);

        // Changing your password should sign out every other session — the stateless access token
        // still has up to its remaining ≤15-minute lifetime, an accepted bounded gap, but no refresh
        // token can silently mint a new one afterward.
        string ip = ipAccessor.IpAddress;
        List<RefreshTokenEntity> activeTokens = await refreshTokens.GetActiveByUserIdAsync(userId, now, cancellationToken);
        foreach (RefreshTokenEntity token in activeTokens)
        {
            token.Revoke(now, ip);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Password changed for user {UserId}; revoked {RevokedCount} refresh token(s)",
            userId, activeTokens.Count);
        return Unit.Value;
    }
}
