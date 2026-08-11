using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.Commands.UpdateResident;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Residents.Commands.UpdateResident;

public class UpdateResidentCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly FlatId AFlatId = new(Guid.NewGuid());

    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdateResidentCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
    }

    private UpdateResidentCommandHandler CreateHandler() => new(
        _residents, _unitOfWork, _currentUser, _clock, Substitute.For<ILogger<UpdateResidentCommandHandler>>());

    private static Resident RegisterResident(Guid tenantId) => Resident.Register(
        tenantId, AFlatId, "Original Name", null, null, ResidentType.Owner, NowUtc);

    [Fact]
    public async Task Updates_Profile_Fields_For_A_Resident_In_Callers_Tenant()
    {
        Resident resident = RegisterResident(TenantId);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        UpdateResidentCommand command = new(resident.Id.Value, "Updated Name", "+8801700000000", "updated@example.com");

        ResidentDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.FullName.Should().Be("Updated Name");
        resident.Phone.Should().Be("+8801700000000");
        resident.Email.Should().Be("updated@example.com");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Resident_Belongs_To_A_Different_Tenant()
    {
        Resident resident = RegisterResident(OtherTenantId);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        UpdateResidentCommand command = new(resident.Id.Value, "Updated Name", null, null);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
