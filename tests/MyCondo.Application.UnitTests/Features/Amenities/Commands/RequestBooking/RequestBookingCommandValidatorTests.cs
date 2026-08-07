using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Amenities.Commands.RequestBooking;

namespace MyCondo.Application.UnitTests.Features.Amenities.Commands.RequestBooking;

public class RequestBookingCommandValidatorTests
{
    private readonly RequestBookingCommandValidator _validator = new();
    private static readonly DateTimeOffset Start = DateTimeOffset.UtcNow.AddDays(10);

    private static RequestBookingCommand ValidCommand() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Birthday party", Start, Start.AddHours(4), 30, 30, 25, true);

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EndAtUtc_Not_After_StartAtUtc_Fails()
    {
        RequestBookingCommand command = ValidCommand() with { EndAtUtc = Start };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestBookingCommand.EndAtUtc));
    }

    [Fact]
    public void Zero_ExpectedGuestCount_Fails()
    {
        RequestBookingCommand command = ValidCommand() with { ExpectedGuestCount = 0 };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestBookingCommand.ExpectedGuestCount));
    }

    [Fact]
    public void Empty_EventType_Fails()
    {
        RequestBookingCommand command = ValidCommand() with { EventType = "" };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestBookingCommand.EventType));
    }
}
