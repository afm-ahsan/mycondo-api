namespace MyCondo.Domain.Features.Payments.Payments;

public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PaymentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new PaymentId(g)
            : throw new FormatException($"Invalid PaymentId: '{s}'");
}
