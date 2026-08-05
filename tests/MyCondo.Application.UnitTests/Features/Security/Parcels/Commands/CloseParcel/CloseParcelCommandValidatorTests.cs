using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.Parcels.Commands.CloseParcel;

namespace MyCondo.Application.UnitTests.Features.Security.Parcels.Commands.CloseParcel;

public class CloseParcelCommandValidatorTests
{
    private readonly CloseParcelCommandValidator _validator = new();

    [Theory]
    [InlineData("Returned")]
    [InlineData("Rejected")]
    [InlineData("LostOrEscalated")]
    public void Valid_Outcome_Passes(string outcome)
    {
        CloseParcelCommand command = new(Guid.NewGuid(), outcome, "Reason");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Collected")]
    [InlineData("Received")]
    [InlineData("NotAStatus")]
    public void Invalid_Outcome_Fails(string outcome)
    {
        CloseParcelCommand command = new(Guid.NewGuid(), outcome, "Reason");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CloseParcelCommand.Outcome));
    }

    [Fact]
    public void Empty_Reason_Fails()
    {
        CloseParcelCommand command = new(Guid.NewGuid(), "Returned", "");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CloseParcelCommand.Reason));
    }
}
