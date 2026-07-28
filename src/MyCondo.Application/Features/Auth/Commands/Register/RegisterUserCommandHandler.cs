using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Auth.Commands.Register;

public sealed class RegisterUserCommandHandler(
    IUserRepository users,
    ITenantRepository tenants,
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionRepository rolePermissions,
    IRoleAssignmentRepository roleAssignments,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUserContextResolver userContextResolver,
    IRequestIpAccessor ipAccessor,
    IClock clock,
    ILogger<RegisterUserCommandHandler> logger
) : IRequestHandler<RegisterUserCommand, AuthTokensDto>
{
    private const string SuperAdminRoleName = "SuperAdmin";

    public async ValueTask<AuthTokensDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.TenantId);

        if (tenant.Status != TenantStatus.Active)
        {
            throw new ForbiddenException($"Tenant '{tenant.Slug}' is not active.");
        }

        string normalizedEmail = command.Email.Trim().ToLowerInvariant();

        bool emailTaken = await users.EmailExistsAsync(command.TenantId, normalizedEmail, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException($"An account with email '{normalizedEmail}' already exists.");
        }

        // Must check before adding the new user below, since Add() alone would make this true.
        bool isFirstUserOfTenant = !await users.AnyForTenantAsync(command.TenantId, cancellationToken);

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

        if (isFirstUserOfTenant)
        {
            await BootstrapSuperAdminAsync(command.TenantId, user, nowUtc, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        AuthenticatedUserDto auth = await userContextResolver.ResolveAsync(user, cancellationToken);
        AuthTokensDto tokens = await tokenService.IssueAsync(auth, ipAccessor.IpAddress, cancellationToken);

        logger.LogInformation("User {UserId} registered on tenant {TenantId}", user.Id, command.TenantId);
        return tokens;
    }

    /// <summary>
    /// The first user to register for a tenant becomes its SuperAdmin — granted every catalogue
    /// permission, so they can bootstrap further roles/users themselves. Runs before SaveChangesAsync
    /// so the issued JWT's `perm` claims (built in userContextResolver.ResolveAsync below) immediately
    /// reflect the grant.
    /// </summary>
    private async Task BootstrapSuperAdminAsync(
        Guid tenantId,
        User user,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        Role superAdmin = Role.CreateSystem(
            RoleId.New(),
            tenantId,
            SuperAdminRoleName,
            "Full access to all permissions (auto-provisioned for the tenant's first user).",
            nowUtc);

        roles.Add(superAdmin);

        List<Permission> catalogue = await permissions.GetAllAsync(cancellationToken);
        foreach (Permission permission in catalogue)
        {
            rolePermissions.Add(new RolePermission(tenantId, superAdmin.Id, permission.Id, nowUtc, grantedBy: null));
        }

        roleAssignments.Add(RoleAssignment.Grant(tenantId, user.Id, superAdmin.Id, buildingId: null, nowUtc));

        logger.LogInformation(
            "SuperAdmin role {RoleId} bootstrapped for tenant {TenantId} with {PermissionCount} permissions",
            superAdmin.Id, tenantId, catalogue.Count);
    }
}
