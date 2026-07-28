using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Auth.Commands.Register;

public sealed class RegisterUserCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUserContextResolver userContextResolver,
    IRequestIpAccessor ipAccessor,
    IClock clock,
    ILogger<RegisterUserCommandHandler> logger
) : IRequestHandler<RegisterUserCommand, AuthTokensDto>
{
    public async ValueTask<AuthTokensDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        string normalizedEmail = command.Email.Trim().ToLowerInvariant();

        bool emailTaken = await users.EmailExistsAsync(command.TenantId, normalizedEmail, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException($"An account with email '{normalizedEmail}' already exists.");
        }

        string passwordHash = passwordHasher.Hash(command.Password);
        DateTimeOffset nowUtc = clock.UtcNow;

        User user = User.Register(
            command.TenantId,
            normalizedEmail,
            passwordHash,
            command.FullName,
            command.PhoneNumber,
            nowUtc);

        users.Add(user);
        user.RecordLogin(ipAccessor.IpAddress, nowUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        AuthenticatedUserDto auth = await userContextResolver.ResolveAsync(user, cancellationToken);
        AuthTokensDto tokens = await tokenService.IssueAsync(auth, ipAccessor.IpAddress, cancellationToken);

        logger.LogInformation("User {UserId} registered on tenant {TenantId}", user.Id, command.TenantId);
        return tokens;
    }
}
