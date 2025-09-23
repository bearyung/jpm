using System.Net;
using Microsoft.Extensions.Configuration;

namespace JinPingMei.Game.Hosting;

public sealed class TelnetHostSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 2323;

    public IPEndPoint ToEndpoint()
    {
        if (!IPAddress.TryParse(Host, out var address))
        {
            address = IPAddress.Any;
        }

        return new IPEndPoint(address, Port);
    }

    public static TelnetHostSettings FromConfiguration(IConfiguration configuration)
    {
        var settings = new TelnetHostSettings();
        configuration.GetSection("Telnet").Bind(settings);
        return settings;
    }
}
