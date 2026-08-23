using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.Commands.GenerateInvoiceBatch;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Services;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.GenerateInvoiceBatch;

/// <summary>
/// Regression coverage for the MVP-1 batch-billing "Flat" defect: skipped/failed batch outcomes must
/// carry a user-facing flat display name, not just the raw <see cref="FlatId"/> the handler already
/// has the <see cref="Flat"/>/<see cref="Building"/> aggregates in hand to format (no extra lookup
/// needed, unlike GetPaymentsQueryHandlerTests/GetInvoicesQueryHandlerTests which resolve via
/// IFlatDisplayNameResolver).
/// </summary>
public class GenerateInvoiceBatchCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IServiceChargeRuleRepository _rules = Substitute.For<IServiceChargeRuleRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IInvoiceSequenceRepository _sequences = Substitute.For<IInvoiceSequenceRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IResponsiblePartyResolver _responsibleParties = Substitute.For<IResponsiblePartyResolver>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GenerateInvoiceBatchCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GenerateInvoiceBatchCommandHandler CreateHandler() => new(
        _buildings, _flats, _rules, _invoices, _sequences, _financialPosting, _responsibleParties,
        _unitOfWork, _currentUser, _clock, Substitute.For<ILogger<GenerateInvoiceBatchCommandHandler>>());

    [Fact]
    public async Task Skipped_Items_Carry_A_Flat_Display_Name_Not_Just_The_Raw_Guid()
    {
        Building building = Building.Create(TenantId, "Aisha Tower", "AISHA", null, Now);
        Flat flat = Flat.Create(TenantId, building.Id, "A-101", null, FlatType.Residential, Now);

        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);
        _flats.GetAllForBuildingAsync(TenantId, building.Id, Arg.Any<CancellationToken>()).Returns([flat]);

        // A Commercial-only rule against a Residential flat leaves flatRules empty — the "no
        // applicable rule for this flat's unit type" skip branch.
        ServiceChargeRule rule = ServiceChargeRule.Create(
            TenantId, building.Id, "Maintenance", "Commercial Maintenance", CalculationMethod.FixedAmount,
            500m, FlatType.Commercial, BillingFrequency.Monthly, new DateOnly(2026, 1, 1), Now);
        _rules.GetApplicableRulesAsync(
                TenantId, building.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([rule]);

        GenerateInvoiceBatchResultDto result = await CreateHandler().Handle(
            new GenerateInvoiceBatchCommand(building.Id.Value, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)),
            CancellationToken.None);

        result.SkippedCount.Should().Be(1);
        BatchItemOutcomeDto skipped = result.SkippedItems.Single();
        skipped.FlatId.Should().Be(flat.Id.Value);
        skipped.FlatDisplayName.Should().Be("AISHA A-101");
    }
}
