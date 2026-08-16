using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Attachments.Commands.UploadAttachment;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Attachments.Commands.UploadAttachment;

public class UploadAttachmentCommandValidatorTests
{
    private readonly IImageValidationService _imageValidation = Substitute.For<IImageValidationService>();
    private UploadAttachmentCommandValidator CreateValidator() => new(_imageValidation);

    private static UploadAttachmentCommand CommandWith(string contentType, long sizeBytes, byte[]? bytes = null)
    {
        MemoryStream content = new(bytes ?? [1, 2, 3]);
        return new UploadAttachmentCommand(content, "Resident", Guid.NewGuid(), "file.bin", contentType, sizeBytes);
    }

    [Fact]
    public async Task Valid_Image_Passes()
    {
        _imageValidation.IsValidImageAsync(Arg.Any<Stream>(), "image/jpeg", Arg.Any<CancellationToken>()).Returns(true);
        UploadAttachmentCommand command = CommandWith("image/jpeg", 1024);

        ValidationResult result = await CreateValidator().ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Valid_Pdf_Signature_Passes()
    {
        byte[] pdfBytes = "%PDF-1.4 rest of file"u8.ToArray();
        UploadAttachmentCommand command = CommandWith("application/pdf", pdfBytes.Length, pdfBytes);

        ValidationResult result = await CreateValidator().ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Pdf_Content_Type_With_Wrong_Signature_Fails()
    {
        byte[] notPdf = "not a pdf file"u8.ToArray();
        UploadAttachmentCommand command = CommandWith("application/pdf", notPdf.Length, notPdf);

        ValidationResult result = await CreateValidator().ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadAttachmentCommand.Content));
    }

    [Fact]
    public async Task Disallowed_Content_Type_Fails()
    {
        UploadAttachmentCommand command = CommandWith("application/zip", 1024);

        ValidationResult result = await CreateValidator().ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadAttachmentCommand.ContentType));
    }

    [Fact]
    public async Task Oversized_File_Fails()
    {
        UploadAttachmentCommand command = CommandWith("application/pdf", (10 * 1024 * 1024) + 1, "%PDF-"u8.ToArray());

        ValidationResult result = await CreateValidator().ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadAttachmentCommand.SizeBytes));
    }

    [Fact]
    public async Task Image_Failing_Signature_Sniff_Fails()
    {
        _imageValidation.IsValidImageAsync(Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>()).Returns(false);
        UploadAttachmentCommand command = CommandWith("image/png", 1024);

        ValidationResult result = await CreateValidator().ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UploadAttachmentCommand.Content));
    }
}
