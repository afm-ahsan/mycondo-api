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

    [Fact]
    public async Task Root_Redirects_To_Scalar_Ui()
    {
        using HttpClient client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be("/scalar");
    }

    [Fact]
    public async Task OpenApi_Document_Is_Served()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
