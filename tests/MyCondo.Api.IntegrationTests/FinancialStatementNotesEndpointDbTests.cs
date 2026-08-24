using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Full-stack, real-Postgres proof that CondoBD Finance Phase 2A Task 5's two "Notes to Accounts"
/// endpoints are actually reachable and correctly bound — including the enum-array <c>notes</c> query
/// parameter, which minimal-API model binding has to parse and which no unit test can exercise. The
/// schedules' arithmetic is covered by <c>FinancialStatementNoteServiceTests</c> and their SQL by
/// <c>FinancialStatementNoteQueryRlsTests</c>; this suite covers wiring, authorization and contract
/// shape. A freshly registered tenant has a seeded chart of accounts but no ledger activity, so every
/// schedule is legitimately zero and reconciled — the empty-but-valid state a real statement starts in.
/// Needs a Docker daemon (PostgreSQL container).
/// </summary>
public class FinancialStatementNotesEndpointDbTests : IClassFixture<PostgresApiFactory>
{
    // Deliberately no JsonStringEnumConverter: the API's established contract serializes enums
    // numerically, so these tests deserialize into the real enum types and assert that contract as-is.
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string PositionNotesUrl =
        "/api/v1/reports/finance/financial-statements/financial-position/notes";
    private const string IncomeExpenditureNotesUrl =
        "/api/v1/reports/finance/financial-statements/income-expenditure/notes";

    private readonly PostgresApiFactory _factory;

    public FinancialStatementNotesEndpointDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record NoteResponse(
        FinancialStatementNoteKey Key,
        string Title,
        FinancialStatementNoteScope Scope,
        FinancialStatementGroup StatementGroup,
        DateOnly? StartDate,
        DateOnly EndDate,
        Guid? FundId,
        decimal GlBalance,
        decimal ScheduleBalance,
        decimal Difference,
        bool IsReconciled,
        decimal UnattributedAmount,
        int TotalRowCount,
        bool IsDetailTruncated,
        IReadOnlyList<string> Warnings);

    private sealed record NotesResponse(
        MetadataResponse Metadata, Guid? FundId, IReadOnlyList<NoteResponse> Notes);

    private sealed record MetadataResponse(
        DateOnly? FromDate, DateOnly? ToDate, DateOnly? AsOfDate, string ScopeSummary,
        string Currency, string AccountingBasis, bool PostedOnly);

    private async Task<string> SeedTenantAndRegisterAsync(string slug)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision($"Tenant {slug}", slug, clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = tenant.Id.Value,
            email = $"owner-{slug}@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Test Owner",
            phoneNumber = (string?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokensDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!.AccessToken;
    }

    private async Task<HttpResponseMessage> GetAsync(string accessToken, string url)
    {
        using HttpClient client = _factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Financial_Position_Notes_Return_Every_Balance_Sheet_Schedule()
    {
        string accessToken = await SeedTenantAndRegisterAsync("notes-position");

        HttpResponseMessage response = await GetAsync(accessToken, $"{PositionNotesUrl}?asOfDate=2026-06-30");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        NotesResponse? body = await response.Content.ReadFromJsonAsync<NotesResponse>(JsonOptions);
        body.Should().NotBeNull();

        body!.Metadata.AsOfDate.Should().Be(new DateOnly(2026, 6, 30));
        body.Metadata.FromDate.Should().BeNull();
        body.Metadata.PostedOnly.Should().BeTrue();

        body.Notes.Select(n => n.Key).Should().BeEquivalentTo(
            IFinancialStatementNoteService.AsOfNoteKeys);
        body.Notes.Should().OnlyContain(n => n.Scope == FinancialStatementNoteScope.AsOfDate && n.StartDate == null);
        body.Notes.Should().OnlyContain(n => n.EndDate == new DateOnly(2026, 6, 30));
        body.Notes.Should().OnlyContain(n =>
            n.GlBalance == 0m && n.ScheduleBalance == 0m && n.Difference == 0m && n.IsReconciled);
    }

    [Fact]
    public async Task Income_Expenditure_Notes_Return_Every_Period_Schedule()
    {
        string accessToken = await SeedTenantAndRegisterAsync("notes-period");

        HttpResponseMessage response = await GetAsync(
            accessToken, $"{IncomeExpenditureNotesUrl}?startDate=2026-06-01&endDate=2026-06-30");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        NotesResponse? body = await response.Content.ReadFromJsonAsync<NotesResponse>(JsonOptions);
        body.Should().NotBeNull();

        body!.Metadata.FromDate.Should().Be(new DateOnly(2026, 6, 1));
        body.Metadata.ToDate.Should().Be(new DateOnly(2026, 6, 30));
        body.Metadata.AsOfDate.Should().BeNull();

        body.Notes.Select(n => n.Key).Should().BeEquivalentTo(
            IFinancialStatementNoteService.PeriodNoteKeys);
        body.Notes.Should().OnlyContain(n => n.Scope == FinancialStatementNoteScope.Period && n.StartDate == new DateOnly(2026, 6, 1));
    }

    [Fact]
    public async Task The_Notes_Query_Parameter_Narrows_The_Response_To_One_Schedule()
    {
        string accessToken = await SeedTenantAndRegisterAsync("notes-filtered");

        HttpResponseMessage response = await GetAsync(
            accessToken, $"{PositionNotesUrl}?asOfDate=2026-06-30&notes=CashAndBank");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        NotesResponse? body = await response.Content.ReadFromJsonAsync<NotesResponse>(JsonOptions);

        body!.Notes.Should().ContainSingle();
        body.Notes[0].Key.Should().Be(FinancialStatementNoteKey.CashAndBank);
        body.Notes[0].StatementGroup.Should().Be(FinancialStatementGroup.CashAndBank);
    }

    [Fact]
    public async Task Repeating_The_Notes_Query_Parameter_Selects_Several_Schedules()
    {
        string accessToken = await SeedTenantAndRegisterAsync("notes-multi");

        HttpResponseMessage response = await GetAsync(
            accessToken, $"{PositionNotesUrl}?asOfDate=2026-06-30&notes=CashAndBank&notes=Payables");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        NotesResponse? body = await response.Content.ReadFromJsonAsync<NotesResponse>(JsonOptions);

        body!.Notes.Select(n => n.Key).Should().BeEquivalentTo(
            [FinancialStatementNoteKey.CashAndBank, FinancialStatementNoteKey.Payables]);
    }

    [Fact]
    public async Task A_Fund_Filter_Is_Echoed_Back_On_Every_Schedule()
    {
        string accessToken = await SeedTenantAndRegisterAsync("notes-fund");
        Guid fundId = Guid.NewGuid();

        HttpResponseMessage response = await GetAsync(
            accessToken, $"{PositionNotesUrl}?asOfDate=2026-06-30&fundId={fundId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        NotesResponse? body = await response.Content.ReadFromJsonAsync<NotesResponse>(JsonOptions);

        body!.FundId.Should().Be(fundId);
        body.Metadata.ScopeSummary.Should().Be("Single fund");
        body.Notes.Should().OnlyContain(n => n.FundId == fundId,
            "a fund-scoped statement must never be explained by an organization-wide schedule");
    }

    [Fact]
    public async Task An_Inverted_Period_Is_Rejected()
    {
        string accessToken = await SeedTenantAndRegisterAsync("notes-badperiod");

        HttpResponseMessage response = await GetAsync(
            accessToken, $"{IncomeExpenditureNotesUrl}?startDate=2026-06-30&endDate=2026-06-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthenticated_Requests_Are_Rejected()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage position = await client.GetAsync($"{PositionNotesUrl}?asOfDate=2026-06-30");
        HttpResponseMessage period = await client.GetAsync(
            $"{IncomeExpenditureNotesUrl}?startDate=2026-06-01&endDate=2026-06-30");

        position.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        period.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
