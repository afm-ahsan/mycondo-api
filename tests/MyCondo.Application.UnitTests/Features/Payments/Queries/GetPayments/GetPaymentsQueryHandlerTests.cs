using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetPayments;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.GetPayments;

/// <summary>
/// Regression coverage for the MVP-1 Payments &amp; Receipts "Flat" column defect: the list query
/// must resolve each payment's <see cref="FlatId"/> to a user-facing display name via
/// <see cref="IFlatDisplayNameResolver"/> rather than leaving the UI to render the raw GUID.
/// </summary>
public class GetPaymentsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IFlatDisplayNameResolver _flatDisplayNames = Substitute.For<IFlatDisplayNameResolver>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetPaymentsQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetPaymentsQueryHandler CreateHandler() => new(_payments, _flatDisplayNames, _currentUser);

    [Fact]
    public async Task Populates_FlatDisplayName_From_Resolver_Not_Raw_Guid()
    {
        Payment payment = Payment.Record(
            TenantId, FlatId, 500m, PaymentMethod.Cash, "REF-1", DateOnly.FromDateTime(Now.UtcDateTime),
            null, LedgerPostingId.New(), Now);

        _payments.SearchAsync(
                TenantId, null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Payment>([payment], 1, 20, 1));

        _flatDisplayNames.ResolveManyAsync(
                Arg.Is<IReadOnlyCollection<FlatId>>(ids => ids.Contains(FlatId)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<FlatId, string> { [FlatId] = "A-101" });

        PagedResult<PaymentDto> result = await CreateHandler().Handle(
            new GetPaymentsQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Items.Single().FlatDisplayName.Should().Be("A-101");
        result.Items.Single().FlatId.Should().Be(FlatId.Value);
    }

    [Fact]
    public async Task Falls_Back_To_Unknown_Flat_When_Resolver_Has_No_Entry()
    {
        Payment payment = Payment.Record(
            TenantId, FlatId, 500m, PaymentMethod.Cash, "REF-1", DateOnly.FromDateTime(Now.UtcDateTime),
            null, LedgerPostingId.New(), Now);

        _payments.SearchAsync(
                TenantId, null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Payment>([payment], 1, 20, 1));

        _flatDisplayNames.ResolveManyAsync(Arg.Any<IReadOnlyCollection<FlatId>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<FlatId, string>());

        PagedResult<PaymentDto> result = await CreateHandler().Handle(
            new GetPaymentsQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Items.Single().FlatDisplayName.Should().Be("Unknown flat");
    }

    [Fact]
    public async Task Throws_Forbidden_When_Unauthenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetPaymentsQuery(null, null, null, null, null, 1, 20), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
