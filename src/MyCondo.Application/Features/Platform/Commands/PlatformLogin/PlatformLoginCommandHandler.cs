using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Commands.PlatformLogin;

public sealed class PlatformLoginCommandHandler(
    IPlatformUserRepository platformUsers,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IPlatformTokenService tokenService,
    IPlatformUserContextResolver contextResolver,
    IPlatformAuditLogRepository auditLog,
    IRequestIpAccessor ipAccessor,
    IClock clock,
    ILogger<PlatformLoginCommandHandler> logger
) : IRequestHandler<PlatformLoginCommand, PlatformAuthTokensDto>
{
    public async ValueTask<PlatformAuthTokensDto> Handle(
        PlatformLoginCommand command, CancellationToken cancellationToken)
    {
        string normalizedEmail = command.Email.Trim().ToLowerInvariant();
        DateTimeOffset nowUtc = clock.UtcNow;
        PlatformUser? user = await platformUsers.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            // Identical message and audit action for unknown email and wrong password — no
            // enumeration leak in the HTTP response. The attempted email is recorded server-side
            // only, in the audit log, for abuse monitoring — it is never echoed back to the caller.
            logger.LogInformation("Failed platform login attempt for {Email}", normalizedEmail);
            auditLog.Add(PlatformAuditLogEntry.Record(
                nowUtc,
                actorPlatformUserId: null,
                action: "platform.login.failed",
                metadata: JsonSerializer.Serialize(new { email = normalizedEmail })));
            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw new ForbiddenException("Invalid email or password.");
        }

        if (user.Status != PlatformUserStatus.Active)
        {
            logger.LogInformation("Platform login blocked: user {PlatformUserId} status {Status}", user.Id, user.Status);
            auditLog.Add(PlatformAuditLogEntry.Record(
                nowUtc, actorPlatformUserId: user.Id.Value, action: "platform.login.failed"));
            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw new ForbiddenException("Account is not active.");
        }

        string ip = ipAccessor.IpAddress;
        user.RecordLogin(nowUtc);

        auditLog.Add(PlatformAuditLogEntry.Record(
            nowUtc, actorPlatformUserId: user.Id.Value, action: "platform.login.succeeded"));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        PlatformAuthenticatedUserDto auth = await contextResolver.ResolveAsync(user, cancellationToken);
        PlatformAuthTokensDto tokens = await tokenService.IssueAsync(auth, ip, cancellationToken);

        logger.LogInformation("Platform user {PlatformUserId} logged in", user.Id);
        return tokens;
    }
}
