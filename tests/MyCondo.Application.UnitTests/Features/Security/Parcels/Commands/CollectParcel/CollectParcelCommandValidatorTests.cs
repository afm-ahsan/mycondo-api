using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.Parcels.Commands.CollectParcel;

namespace MyCondo.Application.UnitTests.Features.Security.Parcels.Commands.CollectParcel;

public class CollectParcelCommandValidatorTests
{
    private readonly CollectParcelCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CollectParcelCommand command = new(Guid.NewGuid(), "Jane Resident", "OTP-1234");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_CollectorName_Fails()
    {
        CollectParcelCommand command = new(Guid.NewGuid(), "", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CollectParcelCommand.CollectorName));
    }
}
