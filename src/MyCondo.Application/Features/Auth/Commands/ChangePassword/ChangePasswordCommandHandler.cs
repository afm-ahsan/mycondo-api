using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUserProvider currentUser,
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

        string newHash = passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newHash, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Password changed for user {UserId}", userId);
        return Unit.Value;
    }
}
