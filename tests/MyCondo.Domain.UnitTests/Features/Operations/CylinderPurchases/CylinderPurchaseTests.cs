using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.CylinderPurchases.Exceptions;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Domain.UnitTests.Features.Operations.CylinderPurchases;

public class CylinderPurchaseTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GasCylinderSupplierId SupplierId = GasCylinderSupplierId.New();

    private static CylinderPurchase RecordPurchase(int quantity = 20, decimal weightKg = 12m, decimal rate = 1500m, decimal delivery = 200m) =>
        CylinderPurchase.Record(
            TenantId, SupplierId, "INV-001", DateOnly.FromDateTime(Now.UtcDateTime), "LPG-12kg", quantity, weightKg,
            rate, delivery, null, Now);

    [Fact]
    public void Record_Starts_PendingApproval_And_Unpaid()
    {
        CylinderPurchase purchase = RecordPurchase();

        purchase.ApprovalStatus.Should().Be(CylinderPurchaseApprovalStatus.PendingApproval);
        purchase.PaymentStatus.Should().Be(CylinderPurchasePaymentStatus.Unpaid);
    }

    [Fact]
    public void Computed_Fields_Match_Server_Calculation_Rules()
    {
        // Quantity=20, CylinderWeightKg=12, RatePerCylinder=1500, DeliveryOrOtherCost=200
        CylinderPurchase purchase = RecordPurchase(20, 12m, 1500m, 200m);

        purchase.TotalKg.Should().Be(240m);              // 20 * 12
        purchase.LineAmount.Should().Be(30000m);          // 20 * 1500
        purchase.UnitPricePerKg.Should().Be(125m);        // 1500 / 12
        purchase.GrandTotal.Should().Be(30200m);          // 30000 + 200
    }

    [Fact]
    public void Record_Throws_When_Quantity_Not_Positive()
    {
        Action act = () => RecordPurchase(quantity: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_Throws_When_CylinderWeightKg_Not_Positive()
    {
        Action act = () => RecordPurchase(weightKg: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Approve_Sets_ApprovedBy_And_ApprovedAtUtc()
    {
        CylinderPurchase purchase = RecordPurchase();
        Guid approver = Guid.NewGuid();

        purchase.Approve(approver, Now);

        purchase.ApprovalStatus.Should().Be(CylinderPurchaseApprovalStatus.Approved);
        purchase.ApprovedBy.Should().Be(approver);
        purchase.ApprovedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Approve_Throws_When_Not_PendingApproval()
    {
        CylinderPurchase purchase = RecordPurchase();
        purchase.Approve(Guid.NewGuid(), Now);

        Action act = () => purchase.Approve(Guid.NewGuid(), Now);

        act.Should().Throw<CylinderPurchaseInvalidTransitionException>();
    }

    [Fact]
    public void Reject_Sets_RejectedReason()
    {
        CylinderPurchase purchase = RecordPurchase();

        purchase.Reject("Price mismatch with agreed rate");

        purchase.ApprovalStatus.Should().Be(CylinderPurchaseApprovalStatus.Rejected);
        purchase.RejectedReason.Should().Be("Price mismatch with agreed rate");
    }

    [Fact]
    public void MarkPaid_Throws_When_Not_Approved()
    {
        CylinderPurchase purchase = RecordPurchase();

        Action act = () => purchase.MarkPaid();

        act.Should().Throw<CylinderPurchaseInvalidTransitionException>();
    }

    [Fact]
    public void MarkPaid_Succeeds_After_Approval()
    {
        CylinderPurchase purchase = RecordPurchase();
        purchase.Approve(Guid.NewGuid(), Now);

        purchase.MarkPaid();

        purchase.PaymentStatus.Should().Be(CylinderPurchasePaymentStatus.Paid);
    }
}
