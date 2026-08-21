using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.Commands.AssessFine;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Services;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.AssessFine;

/// <summary>
/// Handler-level tests for AssessFineCommandHandler — proves it reuses Invoice/InvoiceLine machinery
/// with Source == Fine and posts Dr ResidentReceivable / Cr FineIncome, same NSubstitute pattern as
/// VoidInvoiceCommandHandlerTests/BillReadingCommandHandlerTests.
/// </summary>
public class AssessFineCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IInvoiceSequenceRepository _sequences = Substitute.For<IInvoiceSequenceRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IResponsiblePartyResolver _responsibleParties = Substitute.For<IResponsiblePartyResolver>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AssessFineCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _clock.UtcNow.Returns(Now);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IUnitOfWorkTransaction>());
        _sequences.GetNextValueAsync(TenantId, BuildingId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(1);
        _flats.GetByIdAsync(FlatId, Arg.Any<CancellationToken>())
            .Returns(Flat.Create(TenantId, BuildingId, "A1", null, Domain.Features.Property.Flats.FlatType.Residential, Now));
        _buildings.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>())
            .Returns(Building.Create(TenantId, "Aisha Tower", "AISHA", null, Now));
        StubFinancialPosting();
    }

    private void StubFinancialPosting() =>
        _financialPosting.PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                FinancialPostingRequest request = callInfo.Arg<FinancialPostingRequest>();
                List<LedgerLine> lines = request.Lines
                    .Select(l => new LedgerLine(l.Role, l.FlatId, l.Direction, l.Amount, l.LineDescription ?? request.Description))
                    .ToList();
                (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
                    request.TenantId, request.BusinessDate, request.Description, request.PostingPurpose,
                    request.SourceId, lines, Now);
                return new FinancialPostingResult(posting, entries);
            });

    private AssessFineCommandHandler CreateHandler() => new(
        _flats, _buildings, _invoices, _sequences, _financialPosting, _responsibleParties, _unitOfWork,
        _currentUser, _clock, Substitute.For<ILogger<AssessFineCommandHandler>>());

    [Fact]
    public async Task Assesses_A_Fine_As_A_Fine_Sourced_Invoice_With_FineIncome_Posting()
    {
        AssessFineCommand command = new(FlatId.Value, 500m, "Noise complaint", Today);

        InvoiceDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Source.Should().Be(InvoiceSource.Fine.ToString());
        result.TotalAmount.Should().Be(500m);
        result.Balance.Should().Be(500m);
        result.Status.Should().Be(InvoiceStatus.Issued.ToString());

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "FineAssessment" &&
                r.Lines.Any(l => l.Role == LedgerAccountType.ResidentReceivable && l.FlatId == FlatId && l.Direction == LedgerDirection.Debit && l.Amount == 500m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.FineIncome && l.FlatId == null && l.Direction == LedgerDirection.Credit && l.Amount == 500m)),
            Arg.Any<CancellationToken>());

        _invoices.Received(1).Add(Arg.Any<Invoice>());
    }

    [Fact]
    public async Task Throws_When_Flat_Belongs_To_A_Different_Tenant()
    {
        Flat otherTenantFlat = Flat.Create(Guid.NewGuid(), BuildingId, "A1", null, Domain.Features.Property.Flats.FlatType.Residential, Now);
        _flats.GetByIdAsync(FlatId, Arg.Any<CancellationToken>()).Returns(otherTenantFlat);

        Func<Task> act = () => CreateHandler().Handle(
            new AssessFineCommand(FlatId.Value, 500m, "Noise complaint", Today), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<Exception>();
    }
}
