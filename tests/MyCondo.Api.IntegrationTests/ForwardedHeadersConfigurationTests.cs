using AwesomeAssertions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Unit-level coverage of <c>DependencyInjection.AddForwardedHeadersForTrustedProxy</c>'s
/// configuration-parsing behavior — no HTTP request involved. See UX-6 production-hardening
/// discovery: the safety property under test is that forwarded headers are trusted ONLY once an
/// operator explicitly configures a real proxy/network, never by default.
/// </summary>
public class ForwardedHeadersConfigurationTests
{
    private static ForwardedHeadersOptions BuildOptions(Dictionary<string, string?> configValues)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        ServiceCollection services = [];
        services.AddForwardedHeadersForTrustedProxy(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    [Fact]
    public void No_Configuration_Leaves_The_Secure_Loopback_Only_Default()
    {
        ForwardedHeadersOptions options = BuildOptions([]);
        ForwardedHeadersOptions defaults = new();

        // ASP.NET Core's own default — only loopback (127.0.0.1/::1) is trusted until an operator
        // explicitly widens it. Confirms we never silently trust an arbitrary upstream. Compared by
        // BaseAddress/PrefixLength rather than BeEquivalentTo(defaults.KnownIPNetworks): the struct's
        // internal fields differ enough between two separately-constructed default instances that a
        // full structural comparison is unreliable, even though the public shape is identical.
        options.KnownProxies.Should().BeEquivalentTo(defaults.KnownProxies);
        options.KnownIPNetworks.Select(n => (n.BaseAddress, n.PrefixLength))
            .Should().BeEquivalentTo(defaults.KnownIPNetworks.Select(n => (n.BaseAddress, n.PrefixLength)));
        options.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
    }

    [Fact]
    public void Configured_KnownProxies_Are_Added_To_The_Trusted_List()
    {
        ForwardedHeadersOptions options = BuildOptions(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5",
        });

        options.KnownProxies.Should().Contain(System.Net.IPAddress.Parse("10.0.0.5"));
    }

    [Fact]
    public void Configured_KnownNetworks_Are_Added_To_The_Trusted_List()
    {
        ForwardedHeadersOptions options = BuildOptions(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/8",
        });

        options.KnownIPNetworks.Should().ContainSingle(n =>
            n.BaseAddress.Equals(System.Net.IPAddress.Parse("10.0.0.0")) && n.PrefixLength == 8);
    }

    [Fact]
    public void Malformed_Network_Entries_Are_Ignored_Not_Thrown()
    {
        Action act = () => BuildOptions(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownNetworks:0"] = "not-a-cidr",
            ["ForwardedHeaders:KnownProxies:0"] = "not-an-ip",
        });

        act.Should().NotThrow();
    }
}
