using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.Commands.Login;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Auth.Commands.Login;

/// <summary>
/// Covers the tenant-status check added so a Suspended organization's users can no longer sign in
/// (previously suspension only blocked new self-registration, not existing logins).
/// </summary>
public class LoginCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IUserContextResolver _userContextResolver = Substitute.For<IUserContextResolver>();
    private readonly IRequestIpAccessor _ipAccessor = Substitute.For<IRequestIpAccessor>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public LoginCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
        _ipAccessor.IpAddress.Returns("127.0.0.1");
    }

    private LoginCommandHandler CreateHandler() => new(
        _users, _tenants, _unitOfWork, _passwordHasher, _tokenService, _userContextResolver, _ipAccessor, _clock,
        Substitute.For<ILogger<LoginCommandHandler>>());

    [Fact]
    public async Task Throws_Forbidden_When_Tenant_Is_Suspended_Without_Checking_Password()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        tenant.Activate(NowUtc);
        tenant.Suspend(NowUtc);
        Guid tenantId = tenant.Id.Value;
        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        LoginCommand command = new(tenantId, "admin@mycondo.com", "whatever");

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Organization is not active.");
        await _users.DidNotReceive().GetByEmailAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Tenant_Is_PendingActivation()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        Guid tenantId = tenant.Id.Value;
        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        LoginCommand command = new(tenantId, "admin@mycondo.com", "whatever");

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Organization is not active.");
    }

    [Fact]
    public async Task Throws_Forbidden_When_Tenant_Does_Not_Exist()
    {
        Guid tenantId = Guid.NewGuid();
        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        LoginCommand command = new(tenantId, "admin@mycondo.com", "whatever");

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Succeeds_When_Tenant_And_User_Are_Active_And_Password_Matches()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        tenant.Activate(NowUtc);
        Guid tenantId = tenant.Id.Value;
        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        User user = User.Register(tenantId, "admin@mycondo.com", "hash", "Admin", null, NowUtc);
        _users.GetByEmailAsync(tenantId, "admin@mycondo.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Correct-Password", "hash").Returns(true);

        AuthenticatedUserDto authDto = new(user.Id.Value, tenantId, user.Email, user.FullName, [], [], [], []);
        _userContextResolver.ResolveAsync(user, Arg.Any<CancellationToken>()).Returns(authDto);
        AuthTokensDto tokens = new("access", NowUtc, "refresh", NowUtc, authDto);
        _tokenService.IssueAsync(authDto, "127.0.0.1", Arg.Any<CancellationToken>()).Returns(tokens);

        LoginCommand command = new(tenantId, "admin@mycondo.com", "Correct-Password");

        AuthTokensDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().Be(tokens);
    }
}
