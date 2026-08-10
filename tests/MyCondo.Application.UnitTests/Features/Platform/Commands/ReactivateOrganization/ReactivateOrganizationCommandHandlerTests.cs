using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ReactivateOrganization;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Domain.Features.Tenancy.Exceptions;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.ReactivateOrganization;

public class ReactivateOrganizationCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ReactivateOrganizationCommandHandlerTests() => _clock.UtcNow.Returns(NowUtc);

    private ReactivateOrganizationCommandHandler CreateHandler() =>
        new(_tenants, _unitOfWork, _clock, Substitute.For<ILogger<ReactivateOrganizationCommandHandler>>());

    [Fact]
    public async Task Reactivates_A_Suspended_Organization()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        tenant.Activate(NowUtc);
        tenant.Suspend(NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);

        await CreateHandler().Handle(new ReactivateOrganizationCommand(tenant.Id.Value), CancellationToken.None);

        tenant.Status.Should().Be(TenantStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Organization_Is_Not_Suspended()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);

        Func<Task> act = async () =>
            await CreateHandler().Handle(new ReactivateOrganizationCommand(tenant.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTenantStatusTransitionException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () =>
            await CreateHandler().Handle(new ReactivateOrganizationCommand(organizationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
