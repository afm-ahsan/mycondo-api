namespace MyCondo.Domain.Features.Payments.Idempotency;

public readonly record struct IdempotencyKeyId(Guid Value)
{
    public static IdempotencyKeyId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static IdempotencyKeyId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new IdempotencyKeyId(g)
            : throw new FormatException($"Invalid IdempotencyKeyId: '{s}'");
}
