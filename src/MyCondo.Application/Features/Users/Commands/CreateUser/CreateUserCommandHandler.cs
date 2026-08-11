using System.Security.Cryptography;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IPasswordHasher passwordHasher,
    IClock clock,
    ILogger<CreateUserCommandHandler> logger
) : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string AllChars = UpperChars + LowerChars + DigitChars;

    public async ValueTask<CreateUserResult> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string normalizedEmail = command.Email.Trim().ToLowerInvariant();

        bool emailTaken = await users.EmailExistsAsync(tenantId, normalizedEmail, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException($"An account with email '{normalizedEmail}' already exists.");
        }

        string? generatedPassword = command.InitialPassword is null ? GenerateTemporaryPassword() : null;
        string plainPassword = command.InitialPassword ?? generatedPassword!;
        string passwordHash = passwordHasher.Hash(plainPassword);

        DateTimeOffset nowUtc = clock.UtcNow;

        User user = User.Register(
            tenantId,
            normalizedEmail,
            passwordHash,
            command.FullName,
            command.PhoneNumber,
            nowUtc);

        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} created by administrator for tenant {TenantId}", user.Id, tenantId);

        return new CreateUserResult(user.Id.Value, generatedPassword);
    }

    private static string GenerateTemporaryPassword()
    {
        Span<char> chars = stackalloc char[16];
        chars[0] = UpperChars[RandomNumberGenerator.GetInt32(UpperChars.Length)];
        chars[1] = LowerChars[RandomNumberGenerator.GetInt32(LowerChars.Length)];
        chars[2] = DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)];

        for (int i = 3; i < chars.Length; i++)
        {
            chars[i] = AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)];
        }

        // Fisher-Yates shuffle so the guaranteed-composition characters aren't always at the front.
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int swapIndex = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[swapIndex]) = (chars[swapIndex], chars[i]);
        }

        return new string(chars);
    }
}
