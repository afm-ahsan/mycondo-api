using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MyCondo.Api.IntegrationTests;

public class HealthCheckTests : IClassFixture<MyCondoWebApplicationFactory>
{
    private readonly MyCondoWebApplicationFactory _factory;

    public HealthCheckTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // UX-6 production hardening: Scalar UI, the raw OpenAPI JSON document, and the "/" -> "/scalar"
    // redirect are all mapped only when app.Environment.IsDevelopment() — see Program.cs. This
    // factory boots under the "Testing" environment (see MyCondoWebApplicationFactory's own doc
    // comment), so these routes are unmapped here exactly as they would be in a real non-Development
    // deployment — a real production/staging host must not expose the full internal API surface.

    [Fact]
    public async Task Root_Is_Not_Mapped_Outside_Development()
    {
        using HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OpenApi_Document_Is_Not_Served_Outside_Development()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Scalar_Ui_Is_Not_Served_Outside_Development()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/scalar");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
