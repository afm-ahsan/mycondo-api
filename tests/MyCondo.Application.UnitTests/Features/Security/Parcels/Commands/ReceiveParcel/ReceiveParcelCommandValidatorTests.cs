using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.Parcels.Commands.ReceiveParcel;

namespace MyCondo.Application.UnitTests.Features.Security.Parcels.Commands.ReceiveParcel;

public class ReceiveParcelCommandValidatorTests
{
    private readonly ReceiveParcelCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        ReceiveParcelCommand command = new(
            "REF-1", "Pathao", "TRK-1", "Amazon", Guid.NewGuid(), null, "Package", 1, "Shelf A1");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_RecipientFlatId_Fails()
    {
        ReceiveParcelCommand command = new(null, null, null, null, Guid.Empty, null, "Package", 1, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReceiveParcelCommand.RecipientFlatId));
    }

    [Fact]
    public void Invalid_ParcelType_Fails()
    {
        ReceiveParcelCommand command = new(null, null, null, null, Guid.NewGuid(), null, "NotAType", 1, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReceiveParcelCommand.ParcelType));
    }

    [Fact]
    public void Zero_PackageCount_Fails()
    {
        ReceiveParcelCommand command = new(null, null, null, null, Guid.NewGuid(), null, "Package", 0, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReceiveParcelCommand.PackageCount));
    }
}
