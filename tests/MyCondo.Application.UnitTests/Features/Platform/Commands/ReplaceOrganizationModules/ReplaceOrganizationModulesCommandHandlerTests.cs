using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ReplaceOrganizationModules;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.ReplaceOrganizationModules;

public class ReplaceOrganizationModulesCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly ITenantModuleRepository _tenantModules = Substitute.For<ITenantModuleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentPlatformUserProvider _currentPlatformUser = Substitute.For<ICurrentPlatformUserProvider>();

    public ReplaceOrganizationModulesCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
        _currentPlatformUser.PlatformUserId.Returns(Guid.NewGuid());
    }

    private ReplaceOrganizationModulesCommandHandler CreateHandler() => new(
        _tenants, _tenantModules, _unitOfWork, _clock, _currentPlatformUser,
        Substitute.For<ILogger<ReplaceOrganizationModulesCommandHandler>>());

    [Fact]
    public async Task Delegates_The_Requested_Set_To_The_Repository_And_Saves()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        string[] keys = ["billing", "payments"];

        await CreateHandler().Handle(
            new ReplaceOrganizationModulesCommand(tenant.Id.Value, keys), CancellationToken.None);

        await _tenantModules.Received(1).ReplaceForTenantAsync(
            tenant.Id.Value,
            Arg.Is<IReadOnlyList<string>>(k => k.SequenceEqual(keys)),
            NowUtc,
            _currentPlatformUser.PlatformUserId,
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new ReplaceOrganizationModulesCommand(organizationId, ["billing"]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
