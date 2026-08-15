using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Auth.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IUserContextResolver userContextResolver,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UpdateMyProfileCommandHandler> logger
) : IRequestHandler<UpdateMyProfileCommand, UserProfileDto>
{
    public async ValueTask<UserProfileDto> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userIdValue)
        {
            throw new ForbiddenException("Authentication required.");
        }

        UserId userId = new(userIdValue);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userIdValue);

        user.UpdateProfile(command.FullName, command.PhoneNumber, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Profile updated for user {UserId}", userId);

        return await userContextResolver.ResolveProfileAsync(user, cancellationToken);
    }
}
