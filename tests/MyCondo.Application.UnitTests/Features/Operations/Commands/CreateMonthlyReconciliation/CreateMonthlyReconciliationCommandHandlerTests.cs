using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.Commands.CreateMonthlyReconciliation;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;
using MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Operations.Commands.CreateMonthlyReconciliation;

public class CreateMonthlyReconciliationCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ICylinderStockMovementRepository _movements = Substitute.For<ICylinderStockMovementRepository>();
    private readonly IMonthlyCylinderReconciliationRepository _reconciliations = Substitute.For<IMonthlyCylinderReconciliationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateMonthlyReconciliationCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateMonthlyReconciliationCommandHandler CreateHandler() => new(
        _movements, _reconciliations, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CreateMonthlyReconciliationCommandHandler>>());

    [Fact]
    public async Task Handle_Aggregates_Opening_Received_Issued_EmptyReturned_From_Ledger()
    {
        // August 2026. One receipt before the month (opening stock), then activity within the month.
        DateTimeOffset beforeMonth = new(2026, 7, 20, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset withinMonth1 = new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset withinMonth2 = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset withinMonth3 = new(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

        List<CylinderStockMovement> ledger =
        [
            CylinderStockMovement.Receive(TenantId, "LPG-12kg", 40, beforeMonth, null, null, beforeMonth), // opening = 40
            CylinderStockMovement.Receive(TenantId, "LPG-12kg", 20, withinMonth1, null, null, withinMonth1), // received = 20
            CylinderStockMovement.Issue(TenantId, "LPG-12kg", 15, withinMonth2, null, withinMonth2),         // issued = 15
            CylinderStockMovement.ReturnEmpty(TenantId, "LPG-12kg", 5, withinMonth3, null, withinMonth3),    // emptyReturned = 5
        ];

        _movements.GetForPeriodAsync(TenantId, "LPG-12kg", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        CreateMonthlyReconciliationCommand command = new("LPG-12kg", new DateOnly(2026, 8, 1), null);

        MonthlyCylinderReconciliationDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.OpeningStock.Should().Be(40);
        result.TotalReceived.Should().Be(20);
        result.TotalIssued.Should().Be(15);
        result.TotalEmptyReturned.Should().Be(5);
        // actual closing = opening + sum(period movements) = 40 + (20 - 15 - 5) = 40
        result.ClosingStock.Should().Be(40);
        result.VarianceQuantity.Should().Be(0);
    }
}
