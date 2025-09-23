using JinPingMei.Game.Hosting;
using Microsoft.Extensions.Configuration;

namespace JinPingMei.Engine.Tests;

[Collection(nameof(ConfigurationIsolationCollection))]
public class TelnetHostSettingsTests
{
    [Fact]
    public void FromConfiguration_UsesDefaultsWhenMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var settings = TelnetHostSettings.FromConfiguration(configuration);

        Assert.Equal("127.0.0.1", settings.Host);
        Assert.Equal(2323, settings.Port);
    }

    [Fact]
    public void FromConfiguration_ReadsAppSettingsValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telnet:Host"] = "0.0.0.0",
                ["Telnet:Port"] = "4000"
            })
            .Build();

        var settings = TelnetHostSettings.FromConfiguration(configuration);

        Assert.Equal("0.0.0.0", settings.Host);
        Assert.Equal(4000, settings.Port);
    }

    [Fact]
    public void FromConfiguration_PrefersEnvironmentOverrides()
    {
        const string hostVariable = "TELNET__HOST";
        const string portVariable = "TELNET__PORT";

        var previousHost = Environment.GetEnvironmentVariable(hostVariable);
        var previousPort = Environment.GetEnvironmentVariable(portVariable);

        try
        {
            Environment.SetEnvironmentVariable(hostVariable, "192.168.10.20");
            Environment.SetEnvironmentVariable(portVariable, "4555");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telnet:Host"] = "0.0.0.0",
                ["Telnet:Port"] = "4000"
            })
            .AddEnvironmentVariables()
            .Build();

            var settings = TelnetHostSettings.FromConfiguration(configuration);

            Assert.Equal("192.168.10.20", settings.Host);
            Assert.Equal(4555, settings.Port);
        }
        finally
        {
            Environment.SetEnvironmentVariable(hostVariable, previousHost);
            Environment.SetEnvironmentVariable(portVariable, previousPort);
        }
    }
}

[CollectionDefinition(nameof(ConfigurationIsolationCollection), DisableParallelization = true)]
public sealed class ConfigurationIsolationCollection
{
}
