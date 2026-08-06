namespace MyCondo.Domain.Features.Payments.PaymentAllocations;

public readonly record struct PaymentAllocationId(Guid Value)
{
    public static PaymentAllocationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PaymentAllocationId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new PaymentAllocationId(g)
            : throw new FormatException($"Invalid PaymentAllocationId: '{s}'");
}
