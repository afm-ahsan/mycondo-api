using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Api.Endpoints;
using MyCondo.Application.Features.Attachments.DTOs;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory) covering
/// the Owner household member profile-photo flow: upload → set as primary photo → GET reflects it →
/// unrelated edit preserves it → explicit removal → cross-tenant attachment rejected. Needs a Docker
/// daemon.
/// </summary>
public class HouseholdMemberPrimaryPhotoEndpointsDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03, 0x04];

    private readonly PostgresApiFactory _factory;

    public HouseholdMemberPrimaryPhotoEndpointsDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid TenantId, Guid FlatId)> SeedActiveTenantWithFlatAsync(string slug)
    {
        Guid tenantId;
        DateTimeOffset nowUtc;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            // The `tenants` table itself carries no RLS policy (there is no tenant context yet to
            // scope by, before the first tenant exists), so this insert can go through the regular
            // app-role scope. `buildings`/`flats` are tenant-scoped tables, though, and a DbContext
            // resolved from Services here has no HTTP request to derive a tenant context from — RLS
            // would silently reject those inserts (see CreateDbContextForTenant's docs) — so they're
            // seeded below via a DbContext fixed to this tenant instead.
            ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

            Tenant tenant = Tenant.Provision($"Tenant {slug}", slug, clock.UtcNow);
            tenant.Activate(clock.UtcNow);
            tenants.Add(tenant);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            tenantId = tenant.Id.Value;
            nowUtc = clock.UtcNow;
        }

        await using MyCondoDbContext tenantScoped = _factory.CreateDbContextForTenant(tenantId);
        Building building = Building.Create(tenantId, $"Building {slug}", slug.ToUpperInvariant(), null, nowUtc);
        tenantScoped.Set<Building>().Add(building);

        Flat flat = Flat.Create(tenantId, building.Id, "A-1", 1, FlatType.Residential, nowUtc);
        tenantScoped.Set<Flat>().Add(flat);

        await tenantScoped.SaveChangesAsync();

        return (tenantId, flat.Id.Value);
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

    private static MultipartFormDataContent BuildUploadForm(string ownerType, Guid ownerId, byte[] bytes, string fileName, string contentType)
    {
        MultipartFormDataContent form = new();
        ByteArrayContent fileContent = new(bytes);
        fileContent.Headers.ContentType = new(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(ownerType), "ownerType");
        form.Add(new StringContent(ownerId.ToString()), "ownerId");
        return form;
    }

    private static async Task<(Guid ResidentId, Guid MemberId)> CreateResidentWithHouseholdMemberAsync(
        HttpClient client, string accessToken, Guid flatId)
    {
        using HttpRequestMessage residentRequest = AuthenticatedRequest(HttpMethod.Post, "/api/v1/residents", accessToken);
        residentRequest.Content = JsonContent.Create(new
        {
            flatId,
            fullName = "Jane Owner",
            phone = (string?)null,
            email = (string?)null,
            residentType = "Owner",
        });
        HttpResponseMessage residentResponse = await client.SendAsync(residentRequest);
        residentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement resident = await residentResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Guid residentId = resident.GetProperty("residentId").GetGuid();

        using HttpRequestMessage memberRequest = AuthenticatedRequest(
            HttpMethod.Post, $"/api/v1/residents/{residentId}/household-members", accessToken);
        memberRequest.Content = JsonContent.Create(new
        {
            fullName = "Fatema Ahmed",
            relationshipType = "Spouse",
            gender = "Female",
            dateOfBirth = new DateOnly(1992, 5, 1),
            nationalIdNumber = (string?)null,
            birthCertificateNumber = (string?)null,
            bloodGroup = (string?)null,
            religion = (string?)null,
            nationality = (string?)null,
            occupation = (string?)null,
        });
        HttpResponseMessage memberResponse = await client.SendAsync(memberRequest);
        memberResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ResidentHouseholdMemberDto member = (await memberResponse.Content.ReadFromJsonAsync<ResidentHouseholdMemberDto>(JsonOptions))!;

        return (residentId, member.ResidentHouseholdMemberId);
    }

    [Fact]
    public async Task Set_PrimaryPhoto_Then_Get_Reflects_It_And_Unrelated_Edit_Preserves_It()
    {
        (Guid tenantId, Guid flatId) = await SeedActiveTenantWithFlatAsync("hh-photo-set");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "hh-photo-set@example.com");
        (_, Guid memberId) = await CreateResidentWithHouseholdMemberAsync(client, tokens.AccessToken, flatId);

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/attachments", tokens.AccessToken);
        uploadRequest.Content = BuildUploadForm(
            "ResidentHouseholdMember", memberId, PngBytes, "spouse.png", "image/png");
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AttachmentDto attachment = (await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>(JsonOptions))!;

        using HttpRequestMessage setPhotoRequest = AuthenticatedRequest(
            HttpMethod.Put, $"/api/v1/residents/household-members/{memberId}/primary-photo", tokens.AccessToken);
        setPhotoRequest.Content = JsonContent.Create(new { attachmentId = attachment.AttachmentId });
        HttpResponseMessage setPhotoResponse = await client.SendAsync(setPhotoRequest);
        setPhotoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ResidentHouseholdMemberDto afterSet = (await setPhotoResponse.Content.ReadFromJsonAsync<ResidentHouseholdMemberDto>(JsonOptions))!;
        afterSet.PrimaryPhotoAttachmentId.Should().Be(attachment.AttachmentId);

        // An unrelated field edit (occupation) must not disturb the photo pointer.
        using HttpRequestMessage updateRequest = AuthenticatedRequest(
            HttpMethod.Put, $"/api/v1/residents/household-members/{memberId}", tokens.AccessToken);
        updateRequest.Content = JsonContent.Create(new
        {
            fullName = "Fatema Ahmed",
            relationshipType = "Spouse",
            gender = "Female",
            dateOfBirth = new DateOnly(1992, 5, 1),
            nationalIdNumber = (string?)null,
            birthCertificateNumber = (string?)null,
            bloodGroup = (string?)null,
            religion = (string?)null,
            nationality = (string?)null,
            occupation = "Doctor",
        });
        HttpResponseMessage updateResponse = await client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ResidentHouseholdMemberDto afterUpdate = (await updateResponse.Content.ReadFromJsonAsync<ResidentHouseholdMemberDto>(JsonOptions))!;
        afterUpdate.Occupation.Should().Be("Doctor");
        afterUpdate.PrimaryPhotoAttachmentId.Should().Be(attachment.AttachmentId, "an unrelated field edit must not clear the existing photo");
    }

    [Fact]
    public async Task Explicit_Remove_Clears_PrimaryPhoto()
    {
        (Guid tenantId, Guid flatId) = await SeedActiveTenantWithFlatAsync("hh-photo-remove");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "hh-photo-remove@example.com");
        (_, Guid memberId) = await CreateResidentWithHouseholdMemberAsync(client, tokens.AccessToken, flatId);

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/attachments", tokens.AccessToken);
        uploadRequest.Content = BuildUploadForm(
            "ResidentHouseholdMember", memberId, PngBytes, "spouse.png", "image/png");
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);
        AttachmentDto attachment = (await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>(JsonOptions))!;

        using HttpRequestMessage setPhotoRequest = AuthenticatedRequest(
            HttpMethod.Put, $"/api/v1/residents/household-members/{memberId}/primary-photo", tokens.AccessToken);
        setPhotoRequest.Content = JsonContent.Create(new { attachmentId = attachment.AttachmentId });
        await client.SendAsync(setPhotoRequest);

        using HttpRequestMessage clearRequest = AuthenticatedRequest(
            HttpMethod.Put, $"/api/v1/residents/household-members/{memberId}/primary-photo", tokens.AccessToken);
        clearRequest.Content = JsonContent.Create(new { attachmentId = (Guid?)null });
        HttpResponseMessage clearResponse = await client.SendAsync(clearRequest);

        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ResidentHouseholdMemberDto afterClear = (await clearResponse.Content.ReadFromJsonAsync<ResidentHouseholdMemberDto>(JsonOptions))!;
        afterClear.PrimaryPhotoAttachmentId.Should().BeNull();
    }

    [Fact]
    public async Task Set_PrimaryPhoto_With_Another_Tenants_Attachment_Returns_404()
    {
        (Guid ownerTenantId, Guid ownerFlatId) = await SeedActiveTenantWithFlatAsync("hh-photo-cross-owner");
        (Guid otherTenantId, Guid otherFlatId) = await SeedActiveTenantWithFlatAsync("hh-photo-cross-other");
        using HttpClient client = _factory.CreateClient();
        AuthResponse ownerTokens = await RegisterAsync(client, ownerTenantId, "hh-photo-cross-owner@example.com");
        AuthResponse otherTokens = await RegisterAsync(client, otherTenantId, "hh-photo-cross-other@example.com");
        (_, Guid ownerMemberId) = await CreateResidentWithHouseholdMemberAsync(client, ownerTokens.AccessToken, ownerFlatId);
        (_, Guid otherMemberId) = await CreateResidentWithHouseholdMemberAsync(client, otherTokens.AccessToken, otherFlatId);

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/attachments", otherTokens.AccessToken);
        uploadRequest.Content = BuildUploadForm(
            "ResidentHouseholdMember", otherMemberId, PngBytes, "spouse.png", "image/png");
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);
        AttachmentDto otherAttachment = (await uploadResponse.Content.ReadFromJsonAsync<AttachmentDto>(JsonOptions))!;

        using HttpRequestMessage setPhotoRequest = AuthenticatedRequest(
            HttpMethod.Put, $"/api/v1/residents/household-members/{ownerMemberId}/primary-photo", ownerTokens.AccessToken);
        setPhotoRequest.Content = JsonContent.Create(new { attachmentId = otherAttachment.AttachmentId });
        HttpResponseMessage setPhotoResponse = await client.SendAsync(setPhotoRequest);

        setPhotoResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
