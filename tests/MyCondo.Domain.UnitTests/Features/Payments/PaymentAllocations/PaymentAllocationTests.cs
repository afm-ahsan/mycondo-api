using AwesomeAssertions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Payments.PaymentAllocations;

public class PaymentAllocationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly PaymentId PaymentId = PaymentId.New();
    private static readonly InvoiceId InvoiceId = InvoiceId.New();
    private static readonly FlatId FlatId = FlatId.New();

    [Fact]
    public void Allocate_Sets_Expected_Fields()
    {
        PaymentAllocation allocation = PaymentAllocation.Allocate(TenantId, PaymentId, InvoiceId, FlatId, 500m, Now);

        allocation.TenantId.Should().Be(TenantId);
        allocation.PaymentId.Should().Be(PaymentId);
        allocation.InvoiceId.Should().Be(InvoiceId);
        allocation.FlatId.Should().Be(FlatId);
        allocation.AllocatedAmount.Should().Be(500m);
        allocation.AllocatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Allocate_Throws_When_Amount_Not_Positive()
    {
        Action act = () => PaymentAllocation.Allocate(TenantId, PaymentId, InvoiceId, FlatId, 0m, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Allocate_Throws_When_TenantId_Is_Empty()
    {
        Action act = () => PaymentAllocation.Allocate(Guid.Empty, PaymentId, InvoiceId, FlatId, 500m, Now);

        act.Should().Throw<ArgumentException>();
    }
}
