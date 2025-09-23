using System.Net;
using JinPingMei.Game.Hosting;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var endpoint = new IPEndPoint(IPAddress.Loopback, 2323);
var server = new TelnetGameServer(endpoint);

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("====== JinPingMei AI 遊戲 Telnet Server ======");
Console.WriteLine($"Listening on {endpoint.Address}:{endpoint.Port}. Connect with 'telnet {endpoint.Address} {endpoint.Port}'.");
Console.WriteLine("Press Ctrl+C to shut down.");

try
{
    await server.StartAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Expected during shutdown.
}
finally
{
    await server.StopAsync();
}
