using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInGuest;

namespace MyCondo.Application.UnitTests.Features.Security.AccessSessions.Commands.CheckInGuest;

public class CheckInGuestCommandValidatorTests
{
    private readonly CheckInGuestCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CheckInGuestCommand command = new(
            Guid.NewGuid(), Guid.NewGuid(), "Family visit", Guid.NewGuid(), "QR-1", "Remarks", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_GuestProfileId_Fails()
    {
        CheckInGuestCommand command = new(Guid.Empty, Guid.NewGuid(), null, Guid.NewGuid(), null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckInGuestCommand.GuestProfileId));
    }

    [Fact]
    public void Empty_HostFlatId_Fails()
    {
        CheckInGuestCommand command = new(Guid.NewGuid(), Guid.Empty, null, Guid.NewGuid(), null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckInGuestCommand.HostFlatId));
    }

    [Fact]
    public void Empty_EntryGateId_Fails()
    {
        CheckInGuestCommand command = new(Guid.NewGuid(), Guid.NewGuid(), null, Guid.Empty, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckInGuestCommand.EntryGateId));
    }
}
