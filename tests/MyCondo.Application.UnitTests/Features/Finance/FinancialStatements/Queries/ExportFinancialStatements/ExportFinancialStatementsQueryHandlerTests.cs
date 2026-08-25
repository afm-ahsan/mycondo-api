using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.ExportFinancialStatements;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetFinancialPositionNotes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureNotes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Queries.ExportFinancialStatements;

public class ExportFinancialStatementsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly StartDate = new(2026, 6, 1);
    private static readonly DateOnly EndDate = new(2026, 6, 30);

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReportExportService _csvExportService = Substitute.For<IReportExportService>();
    private readonly IFinancialStatementsPdfRenderer _pdfRenderer = Substitute.For<IFinancialStatementsPdfRenderer>();
    private readonly IFundRepository _funds = Substitute.For<IFundRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public ExportFinancialStatementsQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);

        _sender.Send(Arg.Any<GetStatementOfFinancialPositionQuery>(), Arg.Any<CancellationToken>())
            .Returns(SamplePosition());
        _sender.Send(Arg.Any<GetIncomeExpenditureStatementQuery>(), Arg.Any<CancellationToken>())
            .Returns(SampleIncomeExpenditure());
        _sender.Send(Arg.Any<GetFinancialPositionNotesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new FinancialStatementNotesDto(
                FinanceReportMetadataDto.ForAsOf(EndDate, "Tenant (all funds)", DateTimeOffset.UtcNow, null),
                null,
                [SampleNote(FinancialStatementNoteKey.Payables), SampleNote(FinancialStatementNoteKey.CashAndBank)]));
        _sender.Send(Arg.Any<GetIncomeExpenditureNotesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new FinancialStatementNotesDto(
                FinanceReportMetadataDto.ForPeriod(StartDate, EndDate, "Tenant (all funds)", DateTimeOffset.UtcNow, null),
                null,
                [SampleNote(FinancialStatementNoteKey.InterestIncome)]));
    }

    private ExportFinancialStatementsQueryHandler CreateHandler() =>
        new(_sender, _csvExportService, _pdfRenderer, _funds, _tenants, _currentUser);

    private static StatementOfFinancialPositionDto SamplePosition() => new(
        FinanceReportMetadataDto.ForAsOf(EndDate, "Tenant (all funds)", DateTimeOffset.UtcNow, null),
        null,
        new StatementOfFinancialPositionSectionDto([], 0m),
        new StatementOfFinancialPositionSectionDto([], 0m),
        new StatementOfFinancialPositionSectionDto([], 0m),
        CumulativeSurplusDeficit: 0m,
        TotalAssets: 0m,
        TotalLiabilities: 0m,
        TotalFunds: 0m,
        TotalLiabilitiesAndFunds: 0m,
        Difference: 0m,
        IsBalanced: true,
        UnmappedAccountWarnings: []);

    private static IncomeExpenditureStatementDto SampleIncomeExpenditure() => new(
        FinanceReportMetadataDto.ForPeriod(StartDate, EndDate, "Tenant (all funds)", DateTimeOffset.UtcNow, null),
        null,
        new IncomeExpenditureStatementSectionDto([], 0m),
        new IncomeExpenditureStatementSectionDto([], 0m),
        TotalIncome: 0m,
        TotalExpenditure: 0m,
        SurplusDeficit: 0m,
        UnmappedAccountWarnings: []);

    private static FinancialStatementNote SampleNote(FinancialStatementNoteKey key) => new(
        key,
        key.ToString(),
        FinancialStatementNoteScope.AsOfDate,
        FinancialStatementGroup.CashAndBank,
        null,
        EndDate,
        null,
        GlBalance: 0m,
        ScheduleBalance: 0m,
        Difference: 0m,
        IsReconciled: true,
        UnattributedAmount: 0m,
        TotalRowCount: 0,
        IsDetailTruncated: false,
        Warnings: []);

    [Fact]
    public async Task Csv_Format_Delegates_To_The_Csv_Export_Service()
    {
        ReportExportResult expected = new(new MemoryStream(), "text/csv", "financial-statements-2026-06-01-to-2026-06-30.csv");
        _csvExportService.ExportAsync(
                Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv,
                "financial-statements-2026-06-01-to-2026-06-30", Arg.Any<CancellationToken>())
            .Returns(expected);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFinancialStatementsQuery(StartDate, EndDate, null, ReportExportFormat.Csv), CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _pdfRenderer.DidNotReceiveWithAnyArgs().RenderAsync(
            default!, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Pdf_Format_Delegates_To_The_Pdf_Renderer()
    {
        byte[] pdfBytes = "%PDF-1.4 fake"u8.ToArray();
        _pdfRenderer.RenderAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StatementOfFinancialPositionDto>(),
                Arg.Any<IncomeExpenditureStatementDto>(), Arg.Any<IReadOnlyList<FinancialStatementNote>>(),
                Arg.Any<CancellationToken>())
            .Returns(pdfBytes);

        ReportExportResult result = await CreateHandler().Handle(
            new ExportFinancialStatementsQuery(StartDate, EndDate, null, ReportExportFormat.Pdf), CancellationToken.None);

        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().Be("financial-statements-2026-06-01-to-2026-06-30.pdf");
        await _csvExportService.DidNotReceiveWithAnyArgs().ExportAsync(
            default!, default, default!, default);
    }

    [Fact]
    public async Task Filename_Uses_The_Requested_Start_And_End_Dates_With_No_Extension_Until_Format_Is_Applied()
    {
        _csvExportService.ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "ignored.csv"));

        await CreateHandler().Handle(new ExportFinancialStatementsQuery(StartDate, EndDate, null, ReportExportFormat.Csv), CancellationToken.None);

        await _csvExportService.Received(1).ExportAsync(
            Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv,
            "financial-statements-2026-06-01-to-2026-06-30", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Notes_From_Both_Statements_Are_Merged_And_Ordered_By_The_Durable_NoteKey()
    {
        _pdfRenderer.RenderAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StatementOfFinancialPositionDto>(),
                Arg.Any<IncomeExpenditureStatementDto>(), Arg.Any<IReadOnlyList<FinancialStatementNote>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new ExportFinancialStatementsQuery(StartDate, EndDate, null, ReportExportFormat.Pdf), CancellationToken.None);

        await _pdfRenderer.Received(1).RenderAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<StatementOfFinancialPositionDto>(),
            Arg.Any<IncomeExpenditureStatementDto>(),
            Arg.Is<IReadOnlyList<FinancialStatementNote>>(notes =>
                notes.Select(n => n.Key).SequenceEqual(new[]
                {
                    FinancialStatementNoteKey.CashAndBank,
                    FinancialStatementNoteKey.Payables,
                    FinancialStatementNoteKey.InterestIncome,
                })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task All_Four_Statement_Queries_Are_Sent_Exactly_Once_With_The_Requested_Window_And_Fund()
    {
        Guid fundId = Guid.NewGuid();
        _csvExportService.ExportAsync(Arg.Any<ReportExportDocument>(), ReportExportFormat.Csv, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportExportResult(new MemoryStream(), "text/csv", "ignored.csv"));

        await CreateHandler().Handle(new ExportFinancialStatementsQuery(StartDate, EndDate, fundId, ReportExportFormat.Csv), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<GetStatementOfFinancialPositionQuery>(q => q.AsOfDate == EndDate && q.FundId == fundId), Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<GetIncomeExpenditureStatementQuery>(q => q.StartDate == StartDate && q.EndDate == EndDate && q.FundId == fundId),
            Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<GetFinancialPositionNotesQuery>(q => q.AsOfDate == EndDate && q.FundId == fundId && q.Notes == null),
            Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<GetIncomeExpenditureNotesQuery>(q =>
                q.StartDate == StartDate && q.EndDate == EndDate && q.FundId == fundId && q.Notes == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fund_Label_Falls_Back_To_All_Funds_When_No_Fund_Is_Selected()
    {
        string? capturedFundLabel = null;
        _pdfRenderer.RenderAsync(
                Arg.Any<string>(), Arg.Do<string>(label => capturedFundLabel = label),
                Arg.Any<StatementOfFinancialPositionDto>(), Arg.Any<IncomeExpenditureStatementDto>(),
                Arg.Any<IReadOnlyList<FinancialStatementNote>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new ExportFinancialStatementsQuery(StartDate, EndDate, null, ReportExportFormat.Pdf), CancellationToken.None);

        capturedFundLabel.Should().Be("All Funds");
        await _funds.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task Fund_Label_Uses_The_Resolved_Funds_Name_When_A_Fund_Is_Selected()
    {
        Guid fundId = Guid.NewGuid();
        Fund fund = Fund.Create(TenantId, "RESERVE", "Reserve Fund", null);
        _funds.GetByIdAsync(new FundId(fundId), Arg.Any<CancellationToken>()).Returns(fund);

        string? capturedFundLabel = null;
        _pdfRenderer.RenderAsync(
                Arg.Any<string>(), Arg.Do<string>(label => capturedFundLabel = label),
                Arg.Any<StatementOfFinancialPositionDto>(), Arg.Any<IncomeExpenditureStatementDto>(),
                Arg.Any<IReadOnlyList<FinancialStatementNote>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new ExportFinancialStatementsQuery(StartDate, EndDate, fundId, ReportExportFormat.Pdf), CancellationToken.None);

        capturedFundLabel.Should().Be("Reserve Fund");
    }

    [Fact]
    public async Task Fund_Label_Falls_Back_To_A_Generic_Label_When_The_Fund_Cannot_Be_Resolved()
    {
        Guid fundId = Guid.NewGuid();
        _funds.GetByIdAsync(new FundId(fundId), Arg.Any<CancellationToken>()).Returns((Fund?)null);

        string? capturedFundLabel = null;
        _pdfRenderer.RenderAsync(
                Arg.Any<string>(), Arg.Do<string>(label => capturedFundLabel = label),
                Arg.Any<StatementOfFinancialPositionDto>(), Arg.Any<IncomeExpenditureStatementDto>(),
                Arg.Any<IReadOnlyList<FinancialStatementNote>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new ExportFinancialStatementsQuery(StartDate, EndDate, fundId, ReportExportFormat.Pdf), CancellationToken.None);

        capturedFundLabel.Should().Be("Selected Fund");
    }

    [Fact]
    public async Task Organization_Name_Uses_The_Resolved_Tenants_Name()
    {
        Tenant tenant = Tenant.Provision(new TenantId(TenantId), "Green Valley Condominium", "green-valley", DateTimeOffset.UtcNow);
        _tenants.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        string? capturedOrgName = null;
        _pdfRenderer.RenderAsync(
                Arg.Do<string>(name => capturedOrgName = name), Arg.Any<string>(),
                Arg.Any<StatementOfFinancialPositionDto>(), Arg.Any<IncomeExpenditureStatementDto>(),
                Arg.Any<IReadOnlyList<FinancialStatementNote>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new ExportFinancialStatementsQuery(StartDate, EndDate, null, ReportExportFormat.Pdf), CancellationToken.None);

        capturedOrgName.Should().Be("Green Valley Condominium");
    }

    [Fact]
    public async Task Organization_Name_Falls_Back_To_A_Generic_Label_When_The_Tenant_Cannot_Be_Resolved()
    {
        _tenants.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        string? capturedOrgName = null;
        _pdfRenderer.RenderAsync(
                Arg.Do<string>(name => capturedOrgName = name), Arg.Any<string>(),
                Arg.Any<StatementOfFinancialPositionDto>(), Arg.Any<IncomeExpenditureStatementDto>(),
                Arg.Any<IReadOnlyList<FinancialStatementNote>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new ExportFinancialStatementsQuery(StartDate, EndDate, null, ReportExportFormat.Pdf), CancellationToken.None);

        capturedOrgName.Should().Be("CondoBD");
    }
}
