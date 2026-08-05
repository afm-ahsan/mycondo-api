namespace MyCondo.Domain.Features.Attachments;

public readonly record struct AttachmentId(Guid Value)
{
    public static AttachmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static AttachmentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new AttachmentId(g)
            : throw new FormatException($"Invalid AttachmentId: '{s}'");
}
