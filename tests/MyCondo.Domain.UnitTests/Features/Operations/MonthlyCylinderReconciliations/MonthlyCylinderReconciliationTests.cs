using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;

namespace MyCondo.Domain.UnitTests.Features.Operations.MonthlyCylinderReconciliations;

public class MonthlyCylinderReconciliationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Computes_Zero_Variance_When_Actual_Matches_Expected()
    {
        // opening=100, received=50, issued=30, emptyReturned=10 -> expected closing = 100+50-30-10 = 110
        MonthlyCylinderReconciliation reconciliation = MonthlyCylinderReconciliation.Create(
            TenantId, "LPG-12kg", new DateOnly(2026, 8, 15), 100, 50, 30, 10, 110, null, Guid.NewGuid(), Now);

        reconciliation.ClosingStock.Should().Be(110);
        reconciliation.VarianceQuantity.Should().Be(0);
        reconciliation.PeriodMonth.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void Create_Computes_Nonzero_Variance_When_Actual_Differs_From_Expected()
    {
        // expected closing = 100+50-30-10 = 110, actual counted = 105 -> variance = -5
        MonthlyCylinderReconciliation reconciliation = MonthlyCylinderReconciliation.Create(
            TenantId, "LPG-12kg", new DateOnly(2026, 8, 15), 100, 50, 30, 10, 105, "Physical count short", Guid.NewGuid(), Now);

        reconciliation.VarianceQuantity.Should().Be(-5);
    }
}
