using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.Commands.UpdateHouseholdMember;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Leasing.Commands.UpdateHouseholdMember;

public class UpdateHouseholdMemberCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IHouseholdMemberRepository _members = Substitute.For<IHouseholdMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdateHouseholdMemberCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private UpdateHouseholdMemberCommandHandler CreateHandler() => new(
        _members, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<UpdateHouseholdMemberCommandHandler>>());

    private (HouseholdMember Member, Guid TenantId) SetUpMember()
    {
        Guid tenantId = Guid.NewGuid();
        HouseholdMember member = HouseholdMember.Add(
            tenantId, OccupancyRegistrationId.New(), "John Doe", "Spouse", null, null, null, "Male", null, null,
            null, null, null, NowUtc);

        _currentUser.TenantId.Returns(tenantId);
        _members.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);

        return (member, tenantId);
    }

    [Fact]
    public async Task Updates_Member_When_Valid()
    {
        (HouseholdMember member, _) = SetUpMember();
        UpdateHouseholdMemberCommand command = new(
            member.Id.Value, "Jane Doe", "Spouse", new DateOnly(1990, 1, 1), "01711111111", null, "Female", null,
            "O+", "Islam", "Bangladeshi", "Engineer");

        HouseholdMemberDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.FullName.Should().Be("Jane Doe");
        result.Occupation.Should().Be("Engineer");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Changing_To_Child_Without_Identity()
    {
        (HouseholdMember member, _) = SetUpMember();
        UpdateHouseholdMemberCommand command = new(
            member.Id.Value, "John Doe", "Child", null, null, null, "Male", null, null, null, null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Member_Does_Not_Exist()
    {
        _currentUser.TenantId.Returns(Guid.NewGuid());
        _members.GetByIdAsync(Arg.Any<HouseholdMemberId>(), Arg.Any<CancellationToken>())
            .Returns((HouseholdMember?)null);
        UpdateHouseholdMemberCommand command = new(
            Guid.NewGuid(), "John Doe", "Spouse", null, null, null, "Male", null, null, null, null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Member_Belongs_To_Different_Tenant()
    {
        (HouseholdMember member, _) = SetUpMember();
        _currentUser.TenantId.Returns(Guid.NewGuid());
        UpdateHouseholdMemberCommand command = new(
            member.Id.Value, "John Doe", "Spouse", null, null, null, "Male", null, null, null, null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
