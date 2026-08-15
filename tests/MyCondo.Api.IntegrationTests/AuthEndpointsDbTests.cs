using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Api.Endpoints;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory). These
/// need a Docker daemon and were NOT executed in the environment they were authored in — see
/// PostgresApiFactory's doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class AuthEndpointsDbTests : IClassFixture<PostgresApiFactory>
{
    // The server serializes JSON as camelCase (see MyCondo.Api.DependencyInjection); the response DTOs
    // are plain PascalCase C# records, so deserialization here needs case-insensitive matching —
    // ReadFromJsonAsync<T>() with no options is case-sensitive by default and would silently produce
    // all-default values otherwise.
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public AuthEndpointsDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> SeedActiveTenantAsync(string slug)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision($"Tenant {slug}", slug, clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return tenant.Id.Value;
    }

    private static async Task<AuthResponse> RegisterAsync(
        HttpClient client, Guid tenantId, string email, string fullName = "Test User")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email,
            password = "Correct-Horse-Battery-9",
            fullName,
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

    [Fact]
    public async Task Register_Login_GetProfile_Logout_HappyPath()
    {
        Guid tenantId = await SeedActiveTenantAsync("happy-path");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "owner@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Test Owner",
            phoneNumber = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? registerTokens = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        registerTokens.Should().NotBeNull();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantId,
            email = "owner@example.com",
            password = "Correct-Horse-Battery-9",
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? loginTokens = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        loginTokens.Should().NotBeNull();

        using HttpRequestMessage meRequest = new(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new("Bearer", loginTokens!.AccessToken);
        HttpResponseMessage meResponse = await client.SendAsync(meRequest);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        UserProfileDto? profile = await meResponse.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
        profile!.Email.Should().Be("owner@example.com");

        // No refreshToken in the body — the mycondo_rt cookie set by Register/Login is carried
        // automatically by this HttpClient instance (WebApplicationFactory defaults HandleCookies=true).
        using HttpRequestMessage logoutRequest = new(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Authorization = new("Bearer", loginTokens.AccessToken);
        HttpResponseMessage logoutResponse = await client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Login_With_Correct_Credentials_Succeeds()
    {
        // Standalone regression test for the bug fixed in this slice: Login is AllowAnonymous, so
        // there's no JWT tenant claim yet — before the TenantContextAccessor fallback, RLS's USING
        // clause always evaluated to NULL for anonymous connections, so this query returned zero rows
        // regardless of whether the credentials were correct. Kept separate from the happy-path test
        // above so this specific regression is identifiable on its own, not buried in a longer flow.
        Guid tenantId = await SeedActiveTenantAsync("login-regression");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "login-check@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Login Check",
            phoneNumber = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantId,
            email = "login-check@example.com",
            password = "Correct-Horse-Battery-9",
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? tokens = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_With_Valid_Token_Issues_New_Token_Pair()
    {
        Guid tenantId = await SeedActiveTenantAsync("refresh-round-trip");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "refresh-check@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Refresh Check",
            phoneNumber = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? originalTokens = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        registerResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? originalSetCookie).Should().BeTrue();
        originalSetCookie!.Should().Contain(c => c.StartsWith("mycondo_rt="));

        // No refreshToken in the body — the mycondo_rt cookie set by Register above is carried
        // automatically by this HttpClient instance.
        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { tenantId });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? freshTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        freshTokens.Should().NotBeNull();
        freshTokens!.AccessToken.Should().NotBe(originalTokens!.AccessToken);
        refreshResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? refreshSetCookie).Should().BeTrue();
        refreshSetCookie!.Should().Contain(c => c.StartsWith("mycondo_rt="));
    }

    [Fact]
    public async Task Register_With_Unknown_Tenant_Returns_404()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = Guid.NewGuid(),
            email = "nobody@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Nobody",
            phoneNumber = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_With_Suspended_Tenant_Returns_403()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision("Suspended Co", "suspended-co", clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenant.Suspend(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = tenant.Id.Value,
            email = "someone@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Someone",
            phoneNumber = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_Duplicate_Email_Returns_409()
    {
        Guid tenantId = await SeedActiveTenantAsync("duplicate-email");
        using HttpClient client = _factory.CreateClient();

        object payload = new
        {
            tenantId,
            email = "duplicate@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "First",
            phoneNumber = (string?)null,
        };

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage second = await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProfile_HappyPath_Persists_Name_And_Phone()
    {
        Guid tenantId = await SeedActiveTenantAsync("update-profile");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "update-profile@example.com");

        using HttpRequestMessage updateRequest = AuthenticatedRequest(
            HttpMethod.Put, "/api/v1/auth/me", tokens.AccessToken);
        updateRequest.Content = JsonContent.Create(new { fullName = "Updated Name", phoneNumber = "01700000000" });
        HttpResponseMessage updateResponse = await client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        UserProfileDto? updated = await updateResponse.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
        updated!.FullName.Should().Be("Updated Name");
        updated.PhoneNumber.Should().Be("01700000000");

        using HttpRequestMessage meRequest = AuthenticatedRequest(HttpMethod.Get, "/api/v1/auth/me", tokens.AccessToken);
        HttpResponseMessage meResponse = await client.SendAsync(meRequest);
        UserProfileDto? persisted = await meResponse.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
        persisted!.FullName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProfile_With_Empty_FullName_Returns_400()
    {
        Guid tenantId = await SeedActiveTenantAsync("update-profile-invalid");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "update-profile-invalid@example.com");

        using HttpRequestMessage updateRequest = AuthenticatedRequest(
            HttpMethod.Put, "/api/v1/auth/me", tokens.AccessToken);
        updateRequest.Content = JsonContent.Create(new { fullName = "", phoneNumber = (string?)null });
        HttpResponseMessage updateResponse = await client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_Never_Affects_Another_Users_Row()
    {
        // No user id is ever accepted in the request — the target is resolved solely from the caller's
        // own JWT, so there is nothing to spoof. This confirms that end-to-end: caller A's update never
        // touches caller B's row, even though both belong to the same tenant.
        Guid tenantId = await SeedActiveTenantAsync("update-profile-isolation");
        using HttpClient clientA = _factory.CreateClient();
        using HttpClient clientB = _factory.CreateClient();
        AuthResponse tokensA = await RegisterAsync(clientA, tenantId, "owner-a@example.com", "Owner A");
        AuthResponse tokensB = await RegisterAsync(clientB, tenantId, "owner-b@example.com", "Owner B");

        using HttpRequestMessage updateRequest = AuthenticatedRequest(
            HttpMethod.Put, "/api/v1/auth/me", tokensA.AccessToken);
        updateRequest.Content = JsonContent.Create(new { fullName = "Owner A Renamed", phoneNumber = (string?)null });
        (await clientA.SendAsync(updateRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpRequestMessage meBRequest = AuthenticatedRequest(HttpMethod.Get, "/api/v1/auth/me", tokensB.AccessToken);
        HttpResponseMessage meBResponse = await clientB.SendAsync(meBRequest);
        UserProfileDto? profileB = await meBResponse.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
        profileB!.FullName.Should().Be("Owner B");
    }

    [Fact]
    public async Task UploadAvatar_HappyPath_Is_Reflected_In_Profile_And_Downloadable()
    {
        Guid tenantId = await SeedActiveTenantAsync("upload-avatar");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "avatar@example.com");

        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03, 0x04];
        using MultipartFormDataContent form = new();
        using ByteArrayContent fileContent = new(pngBytes);
        fileContent.Headers.ContentType = new("image/png");
        form.Add(fileContent, "file", "avatar.png");

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/auth/me/avatar", tokens.AccessToken);
        uploadRequest.Content = form;
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        UserProfileDto? updated = await uploadResponse.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
        updated!.AvatarUrl.Should().Be("/api/v1/auth/me/avatar");

        using HttpRequestMessage downloadRequest = AuthenticatedRequest(
            HttpMethod.Get, "/api/v1/auth/me/avatar", tokens.AccessToken);
        HttpResponseMessage downloadResponse = await client.SendAsync(downloadRequest);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await downloadResponse.Content.ReadAsByteArrayAsync()).Should().Equal(pngBytes);
    }

    [Fact]
    public async Task UploadAvatar_With_Mislabeled_File_Returns_400()
    {
        // Declares image/png but the bytes are plain text — IImageValidationService's magic-byte sniff
        // must reject this even though the Content-Type header alone claims it's fine.
        Guid tenantId = await SeedActiveTenantAsync("upload-avatar-invalid");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "avatar-invalid@example.com");

        byte[] notAnImage = "#!/bin/sh\necho not an image"u8.ToArray();
        using MultipartFormDataContent form = new();
        using ByteArrayContent fileContent = new(notAnImage);
        fileContent.Headers.ContentType = new("image/png");
        form.Add(fileContent, "file", "fake.png");

        using HttpRequestMessage uploadRequest = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/auth/me/avatar", tokens.AccessToken);
        uploadRequest.Content = form;
        HttpResponseMessage uploadResponse = await client.SendAsync(uploadRequest);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveAvatar_Clears_The_AvatarUrl()
    {
        Guid tenantId = await SeedActiveTenantAsync("remove-avatar");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "remove-avatar@example.com");

        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        using MultipartFormDataContent form = new();
        using ByteArrayContent fileContent = new(pngBytes);
        fileContent.Headers.ContentType = new("image/png");
        form.Add(fileContent, "file", "avatar.png");
        using HttpRequestMessage uploadRequest = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/auth/me/avatar", tokens.AccessToken);
        uploadRequest.Content = form;
        (await client.SendAsync(uploadRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpRequestMessage removeRequest = AuthenticatedRequest(
            HttpMethod.Delete, "/api/v1/auth/me/avatar", tokens.AccessToken);
        HttpResponseMessage removeResponse = await client.SendAsync(removeRequest);

        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        UserProfileDto? updated = await removeResponse.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
        updated!.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task ChangePassword_Revokes_The_Refresh_Token_From_Another_Session()
    {
        // Regression coverage for the session-invalidation behavior added alongside My Profile: a
        // password change must sign out every other session, not just update the hash.
        Guid tenantId = await SeedActiveTenantAsync("change-password-revokes");
        using HttpClient client = _factory.CreateClient();
        AuthResponse tokens = await RegisterAsync(client, tenantId, "change-password@example.com");

        using HttpRequestMessage changePasswordRequest = AuthenticatedRequest(
            HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken);
        changePasswordRequest.Content = JsonContent.Create(new
        {
            currentPassword = "Correct-Horse-Battery-9",
            newPassword = "Even-Better-Horse-9",
        });
        HttpResponseMessage changePasswordResponse = await client.SendAsync(changePasswordRequest);
        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The refresh-token cookie Register issued is revoked by the password change above — carried
        // automatically by this same HttpClient instance, so its next refresh attempt must fail.
        HttpResponseMessage refreshAfterChange = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { tenantId });
        refreshAfterChange.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
