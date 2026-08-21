namespace MyCondo.Domain.Features.Finance.BankReconciliations;

public readonly record struct BankReconciliationId(Guid Value)
{
    public static BankReconciliationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
