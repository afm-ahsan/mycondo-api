using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Api.Endpoints;
using MyCondo.Application.Features.Attachments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory). These
/// need a Docker daemon and were NOT executed in the environment they were authored in (Docker
/// Desktop's daemon wasn't running there) — run wherever Docker is available before trusting them.
/// </summary>
public class AttachmentEndpointsDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public AttachmentEndpointsDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid TenantId, Guid BuildingId)> SeedActiveTenantWithBuildingAsync(string slug)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IBuildingRepository buildings = scope.ServiceProvider.GetRequiredService<IBuildingRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision($"Tenant {slug}", slug, clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenants.Add(tenant);

        Building building = Building.Create(tenant.Id.Value, $"Building {slug}", slug.ToUpperInvariant(), null, clock.UtcNow);
        buildings.Add(building);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return (tenant.Id.Value, building.Id.Value);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, Guid tenantId, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email,
            password = "Correct-Horse-Battery-9",
            fullName = "Test Admin",
            phoneNumber = (string?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? tokens = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!;
    }

    private static HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string accessToken)
    {
        HttpRequestMessage request = new(method, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        return request;
    }

    private static MultipartFormDataContent BuildUploadForm(Guid buildingId, byte[] bytes, string fileName, string contentType)
    {
        MultipartFormDataContent form = new();
        ByteArrayContent fileContent = new(bytes);
        fileContent.Headers.ContentType = new(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent("Building"), "ownerType");
        form.Add(new StringContent(buildingId.ToString()), "ownerId");
        return form;
    }

    [Fact]
    public async Task Upload_Then_Download_Round_Trips_The_Exact_Bytes()
    {
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync("attach-roundtrip");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "attach-roundtrip@example.com");

        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03, 0x04];
        using HttpRequestMessage uploadRequest = AuthenticatedRequest(HttpMethod.Post, "/api/v1/attachments", tokens.AccessToken);
        uploadRequest.Content = BuildUploadForm(buildingId, pngBytes, "building.png", "image/png");
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AttachmentDto? recorded = await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>(JsonOptions);
        recorded.Should().NotBeNull();
        // A caller-controlled name would let a client point the record at an arbitrary path; the
        // server must always mint its own key.
        recorded!.StorageKey.Should().NotBe("building.png");

        using HttpRequestMessage downloadRequest = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/attachments/{recorded.AttachmentId}/content", tokens.AccessToken);
        HttpResponseMessage downloadResponse = await client.SendAsync(downloadRequest);

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await downloadResponse.Content.ReadAsByteArrayAsync()).Should().Equal(pngBytes);
    }

    [Fact]
    public async Task Download_By_Another_Tenant_Returns_404()
    {
        (Guid ownerTenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync("attach-owner");
        (Guid otherTenantId, _) = await SeedActiveTenantWithBuildingAsync("attach-other");
        using HttpClient client = _factory.CreateClient();
        AuthResponse ownerTokens = await RegisterAsync(client, ownerTenantId, "attach-owner@example.com");
        AuthResponse otherTokens = await RegisterAsync(client, otherTenantId, "attach-other@example.com");

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(HttpMethod.Post, "/api/v1/attachments", ownerTokens.AccessToken);
        uploadRequest.Content = BuildUploadForm(buildingId, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "building.png", "image/png");
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);
        AttachmentDto recorded = (await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>(JsonOptions))!;

        using HttpRequestMessage downloadRequest = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/attachments/{recorded.AttachmentId}/content", otherTokens.AccessToken);
        HttpResponseMessage downloadResponse = await client.SendAsync(downloadRequest);

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Then_Download_Returns_404()
    {
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync("attach-delete");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "attach-delete@example.com");

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(HttpMethod.Post, "/api/v1/attachments", tokens.AccessToken);
        uploadRequest.Content = BuildUploadForm(buildingId, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "building.png", "image/png");
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);
        AttachmentDto recorded = (await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>(JsonOptions))!;

        using HttpRequestMessage deleteRequest = AuthenticatedRequest(
            HttpMethod.Delete, $"/api/v1/attachments/{recorded.AttachmentId}", tokens.AccessToken);
        HttpResponseMessage deleteResponse = await client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using HttpRequestMessage downloadRequest = AuthenticatedRequest(
            HttpMethod.Get, $"/api/v1/attachments/{recorded.AttachmentId}/content", tokens.AccessToken);
        HttpResponseMessage downloadResponse = await client.SendAsync(downloadRequest);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Upload_With_Disallowed_Content_Type_Returns_400()
    {
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync("attach-badtype");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "attach-badtype@example.com");

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(HttpMethod.Post, "/api/v1/attachments", tokens.AccessToken);
        uploadRequest.Content = BuildUploadForm(buildingId, [0x00, 0x01], "malware.exe", "application/octet-stream");
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_Against_Another_Tenants_Building_Returns_404()
    {
        (Guid tenantId, _) = await SeedActiveTenantWithBuildingAsync("attach-cross-owner");
        (_, Guid otherBuildingId) = await SeedActiveTenantWithBuildingAsync("attach-cross-other");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "attach-cross-owner@example.com");

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(HttpMethod.Post, "/api/v1/attachments", tokens.AccessToken);
        uploadRequest.Content = BuildUploadForm(otherBuildingId, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "building.png", "image/png");
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
