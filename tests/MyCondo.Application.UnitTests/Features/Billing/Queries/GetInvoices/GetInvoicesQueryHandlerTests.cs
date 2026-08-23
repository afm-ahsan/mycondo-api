using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Queries.GetInvoices;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Billing.Queries.GetInvoices;

/// <summary>
/// Regression coverage for the MVP-1 Billing "Flat" column defect (same class of bug as
/// GetPaymentsQueryHandlerTests): the invoice list query must resolve each invoice's
/// <see cref="FlatId"/> to a user-facing display name via <see cref="IFlatDisplayNameResolver"/>.
/// </summary>
public class GetInvoicesQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IFlatDisplayNameResolver _flatDisplayNames = Substitute.For<IFlatDisplayNameResolver>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetInvoicesQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetInvoicesQueryHandler CreateHandler() => new(_invoices, _flatDisplayNames, _currentUser);

    private static Invoice IssuedInvoice()
    {
        InvoiceLineInput line = new(
            ServiceChargeRuleId.New(), "Standard Charge", "ServiceCharge", "FixedAmount", 1800m, null, 1m,
            1800m, "Standard Charge (ServiceCharge)");
        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, "INV-TEST-2026-000001", InvoiceSource.ServiceCharge,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31),
            [line], LedgerPostingId.New(), LedgerAccountType.AssociationRevenue, null, Now);
        return invoice;
    }

    [Fact]
    public async Task Populates_FlatDisplayName_From_Resolver_Not_Raw_Guid()
    {
        Invoice invoice = IssuedInvoice();
        _invoices.SearchAsync(
                TenantId, null, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Invoice>([invoice], 1, 20, 1));

        _flatDisplayNames.ResolveManyAsync(
                Arg.Is<IReadOnlyCollection<FlatId>>(ids => ids.Contains(FlatId)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<FlatId, string> { [FlatId] = "A-101" });

        PagedResult<InvoiceDto> result = await CreateHandler().Handle(
            new GetInvoicesQuery(null, null, null, null, 1, 20), CancellationToken.None);

        result.Items.Single().FlatDisplayName.Should().Be("A-101");
        result.Items.Single().FlatId.Should().Be(FlatId.Value);
    }

    [Fact]
    public async Task Throws_Forbidden_When_Unauthenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetInvoicesQuery(null, null, null, null, 1, 20), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
