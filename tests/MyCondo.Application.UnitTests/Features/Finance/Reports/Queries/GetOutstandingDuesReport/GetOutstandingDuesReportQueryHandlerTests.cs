using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetOutstandingDuesReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetOutstandingDuesReport;

public class GetOutstandingDuesReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly BuildingId BuildingId = new(Guid.NewGuid());

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetOutstandingDuesReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetOutstandingDuesReportQueryHandler CreateHandler() => new(_invoices, _flats, _currentUser, _clock);

    [Fact]
    public async Task Outstanding_Balance_Is_Grouped_And_Summed_Per_Flat()
    {
        FlatId flatA = new(Guid.NewGuid());
        FlatId flatB = new(Guid.NewGuid());
        Flat flat = Flat.Create(TenantId, BuildingId, "A-101", 1, FlatType.Residential, Now);

        _invoices.GetOpenReceivablesByFlatAsync(TenantId, null, Arg.Any<CancellationToken>()).Returns(
        [
            new FlatReceivableLine(flatA, 5_000m, 2),
            new FlatReceivableLine(flatB, 1_500m, 1),
        ]);
        _flats.GetByIdsAsync(Arg.Any<IReadOnlyCollection<FlatId>>(), Arg.Any<CancellationToken>())
            .Returns([flat]);

        OutstandingDuesReportDto result = await CreateHandler().Handle(new GetOutstandingDuesReportQuery(null), CancellationToken.None);

        result.Lines.Should().HaveCount(2);
        result.TotalOutstanding.Should().Be(6_500m);
        result.Lines.Should().Contain(l => l.FlatId == flatA.Value && l.OutstandingBalance == 5_000m && l.OpenInvoiceCount == 2);
    }

    [Fact]
    public async Task Flats_With_Zero_Balance_Are_Excluded()
    {
        FlatId flatA = new(Guid.NewGuid());
        _invoices.GetOpenReceivablesByFlatAsync(TenantId, null, Arg.Any<CancellationToken>()).Returns(
        [
            new FlatReceivableLine(flatA, 0m, 0),
        ]);
        _flats.GetByIdsAsync(Arg.Any<IReadOnlyCollection<FlatId>>(), Arg.Any<CancellationToken>()).Returns([]);

        OutstandingDuesReportDto result = await CreateHandler().Handle(new GetOutstandingDuesReportQuery(null), CancellationToken.None);

        result.Lines.Should().BeEmpty();
        result.TotalOutstanding.Should().Be(0m);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new GetOutstandingDuesReportQuery(null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
