namespace MyCondo.Domain.Features.Finance.BankReconciliations;

public readonly record struct BankStatementLineId(Guid Value)
{
    public static BankStatementLineId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
