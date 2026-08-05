using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Attachments.Commands.RecordAttachment;

namespace MyCondo.Application.UnitTests.Features.Attachments.Commands.RecordAttachment;

public class RecordAttachmentCommandValidatorTests
{
    private readonly RecordAttachmentCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        RecordAttachmentCommand command = new("Resident", Guid.NewGuid(), "s3://key", "id-card.jpg", "image/jpeg", 1024);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_OwnerId_Fails()
    {
        RecordAttachmentCommand command = new("Resident", Guid.Empty, "s3://key", "id-card.jpg", "image/jpeg", 1024);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordAttachmentCommand.OwnerId));
    }

    [Fact]
    public void Invalid_OwnerType_Fails()
    {
        RecordAttachmentCommand command = new("NotARealType", Guid.NewGuid(), "s3://key", "id-card.jpg", "image/jpeg", 1024);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordAttachmentCommand.OwnerType));
    }

    [Fact]
    public void Zero_SizeBytes_Fails()
    {
        RecordAttachmentCommand command = new("Resident", Guid.NewGuid(), "s3://key", "id-card.jpg", "image/jpeg", 0);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordAttachmentCommand.SizeBytes));
    }
}
