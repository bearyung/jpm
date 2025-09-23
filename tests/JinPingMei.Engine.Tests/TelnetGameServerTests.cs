using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using JinPingMei.Game.Hosting;
using JinPingMei.Game.Localization;
using Microsoft.Extensions.Logging;

namespace JinPingMei.Engine.Tests;

public class TelnetGameServerTests
{
    private readonly TestLogger<TelnetGameServer> _logger = new();
    private readonly TestMetrics _metrics = new();
    private readonly ILocalizationProvider _localization = new TestLocalizationProvider();

    [Fact]
    public async Task Connect_WhenServerAtCapacity_ReceivesBusyMessage()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, GetFreeTcpPort());
        var options = new TelnetGameServerOptions
        {
            MaxConcurrentSessions = 1,
            BusyMessage = "Server busy",
            SessionIdleTimeout = TimeSpan.FromSeconds(30)
        };

        var server = CreateServer(endpoint, options);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serverTask = server.StartAsync(cts.Token);

        using var primaryClient = await ConnectAsync(endpoint);
        using var primaryReader = CreateReader(primaryClient);

        await ReadIntroAsync(primaryReader);

        using var secondaryClient = await ConnectAsync(endpoint);
        using var secondaryReader = CreateReader(secondaryClient);

        await AssertBusyAsync(secondaryReader, options.BusyMessage);

        Assert.Equal(1, _metrics.Rejections);
        Assert.Contains(_logger.Entries, e => e.Message.Contains("reject", StringComparison.OrdinalIgnoreCase));

        primaryClient.Close();
        cts.Cancel();
        await SafeStopAsync(server, serverTask);
    }

    [Fact]
    public async Task ClientIdleBeyondTimeout_DisconnectsWithNotification()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, GetFreeTcpPort());
        var options = new TelnetGameServerOptions
        {
            SessionIdleTimeout = TimeSpan.FromMilliseconds(200),
            InactivityMessage = "Inactive disconnect"
        };

        var server = CreateServer(endpoint, options);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serverTask = server.StartAsync(cts.Token);

        using var client = await ConnectAsync(endpoint);
        using var reader = CreateReader(client);

        await ReadIntroAsync(reader);

        var timeoutLine = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Contains(options.InactivityMessage, timeoutLine ?? string.Empty);
        Assert.Equal(1, _metrics.InactivityTimeouts);

        cts.Cancel();
        await SafeStopAsync(server, serverTask);
    }

    [Fact]
    public async Task IdleSession_ReceivesHeartbeatBeforeTimeout()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, GetFreeTcpPort());
        var options = new TelnetGameServerOptions
        {
            SessionIdleTimeout = TimeSpan.FromMilliseconds(500),
            HeartbeatInterval = TimeSpan.FromMilliseconds(150),
            HeartbeatMessage = "[heartbeat]"
        };

        var server = CreateServer(endpoint, options);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serverTask = server.StartAsync(cts.Token);

        using var client = await ConnectAsync(endpoint);
        using var reader = CreateReader(client);

        await ReadIntroAsync(reader);

        var heartbeatLine = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Contains(options.HeartbeatMessage, heartbeatLine ?? string.Empty);

        cts.Cancel();
        await SafeStopAsync(server, serverTask);
    }

    [Fact]
    public async Task SessionLifetimeLimit_EndsSessionEvenWithActivity()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, GetFreeTcpPort());
        var options = new TelnetGameServerOptions
        {
            SessionIdleTimeout = TimeSpan.FromSeconds(5),
            SessionLifetime = TimeSpan.FromMilliseconds(300),
            LifetimeExceededMessage = "Session lifetime exceeded"
        };

        var server = CreateServer(endpoint, options);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serverTask = server.StartAsync(cts.Token);

        using var client = await ConnectAsync(endpoint);
        using var reader = CreateReader(client);
        using var writer = CreateWriter(client);

        await ReadIntroAsync(reader);

        await writer.WriteLineAsync("/help");
        var helpLine1 = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(1));
        var helpLine2 = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Contains("/help", helpLine1 ?? string.Empty);
        Assert.False(string.IsNullOrWhiteSpace(helpLine2));

        var lifetimeLine = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Contains(options.LifetimeExceededMessage, lifetimeLine ?? string.Empty);
        Assert.Equal(1, _metrics.LifetimeExpirations);

        cts.Cancel();
        await SafeStopAsync(server, serverTask);
    }

    [Fact]
    public async Task LifecycleEvents_AreLogged()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, GetFreeTcpPort());
        var options = new TelnetGameServerOptions
        {
            SessionIdleTimeout = TimeSpan.FromSeconds(1)
        };

        var server = CreateServer(endpoint, options);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serverTask = server.StartAsync(cts.Token);

        using var client = await ConnectAsync(endpoint);
        using var reader = CreateReader(client);

        await ReadIntroAsync(reader);

        client.Close();

        await Task.Delay(100);

        Assert.Contains(_logger.Entries, e => e.Message.Contains("accepted", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(_logger.Entries, e => e.Message.Contains("ended", StringComparison.OrdinalIgnoreCase));

        cts.Cancel();
        await SafeStopAsync(server, serverTask);
    }

    private static async Task<TcpClient> ConnectAsync(IPEndPoint endpoint)
    {
        var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port);
        return client;
    }

    private static StreamReader CreateReader(TcpClient client)
    {
        return new StreamReader(client.GetStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
    }

    private static StreamWriter CreateWriter(TcpClient client)
    {
        return new StreamWriter(client.GetStream(), Encoding.UTF8, bufferSize: 1024, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };
    }

    private static async Task ReadIntroAsync(StreamReader reader)
    {
        for (var i = 0; i < 5; i++)
        {
            await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    private static async Task AssertBusyAsync(StreamReader reader, string expectedMessage)
    {
        var line = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Contains(expectedMessage, line ?? string.Empty);
    }

    private static async Task SafeStopAsync(TelnetGameServer server, Task serverTask)
    {
        try
        {
            await serverTask;
        }
        catch (OperationCanceledException)
        {
        }

        await server.StopAsync();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private TelnetGameServer CreateServer(IPEndPoint endpoint, TelnetGameServerOptions options)
    {
        var sessionFactory = new GameSessionFactory(_localization);
        return new TelnetGameServer(endpoint, options, _logger, _metrics, sessionFactory);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception)));
        }

        public record LogEntry(LogLevel Level, EventId EventId, string Message);
    }

    private sealed class TestMetrics : ITelnetServerMetrics
    {
        public int AcceptedSessions { get; private set; }
        public int Rejections { get; private set; }
        public int InactivityTimeouts { get; private set; }
        public int LifetimeExpirations { get; private set; }

        public void RecordAccepted() => AcceptedSessions++;
        public void RecordRejected() => Rejections++;
        public void RecordInactivityTimeout() => InactivityTimeouts++;
        public void RecordLifetimeLimit() => LifetimeExpirations++;
    }

}
