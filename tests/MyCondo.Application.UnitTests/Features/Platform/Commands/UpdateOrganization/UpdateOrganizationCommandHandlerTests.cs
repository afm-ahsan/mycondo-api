using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.UpdateOrganization;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.UpdateOrganization;

public class UpdateOrganizationCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdateOrganizationCommandHandlerTests() => _clock.UtcNow.Returns(NowUtc);

    private UpdateOrganizationCommandHandler CreateHandler() =>
        new(_tenants, _unitOfWork, _clock, Substitute.For<ILogger<UpdateOrganizationCommandHandler>>());

    [Fact]
    public async Task Updates_Name_And_Code()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _tenants.CodeExistsAsync("NEW", Arg.Any<CancellationToken>()).Returns(false);

        await CreateHandler().Handle(
            new UpdateOrganizationCommand(tenant.Id.Value, "New Name", "new"), CancellationToken.None);

        tenant.Name.Should().Be("New Name");
        tenant.Code.Should().Be("NEW");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_New_Code_Already_Taken_By_Another_Organization()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _tenants.CodeExistsAsync("TAKEN", Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(
            new UpdateOrganizationCommand(tenant.Id.Value, "New Name", "taken"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Does_Not_Check_Uniqueness_When_Code_Is_Unchanged()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        tenant.UpdateDetails("ARP", "ARP", NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);

        await CreateHandler().Handle(
            new UpdateOrganizationCommand(tenant.Id.Value, "ARP Renamed", "arp"), CancellationToken.None);

        await _tenants.DidNotReceive().CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        tenant.Name.Should().Be("ARP Renamed");
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new UpdateOrganizationCommand(organizationId, "Name", "CODE"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
