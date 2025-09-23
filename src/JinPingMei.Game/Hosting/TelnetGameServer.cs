using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JinPingMei.Engine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JinPingMei.Game.Hosting;

public sealed class TelnetGameServer
{
    public const string ActivitySourceName = "JinPingMei.Game.Hosting.TelnetGameServer";

    private readonly IPEndPoint _endpoint;
    private readonly TelnetGameServerOptions _options;
    private readonly ILogger<TelnetGameServer> _logger;
    private readonly ITelnetServerMetrics _metrics;
    private TcpListener? _listener;
    private readonly List<Task> _clientTasks = new();
    private readonly object _sync = new();
    private readonly SemaphoreSlim _sessionSlots;
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public TelnetGameServer(
        IPEndPoint endpoint,
        TelnetGameServerOptions? options = null,
        ILogger<TelnetGameServer>? logger = null,
        ITelnetServerMetrics? metrics = null)
    {
        _endpoint = endpoint;
        _options = options ?? new TelnetGameServerOptions();
        _logger = logger ?? NullLogger<TelnetGameServer>.Instance;
        _metrics = metrics ?? new NullTelnetServerMetrics();

        if (_options.MaxConcurrentSessions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options) + "." + nameof(TelnetGameServerOptions.MaxConcurrentSessions), "Max concurrent sessions must be greater than zero.");
        }

        _sessionSlots = new SemaphoreSlim(_options.MaxConcurrentSessions, _options.MaxConcurrentSessions);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("Server is already running.");
        }

        _listener = new TcpListener(_endpoint);
        _listener.Start();

        _logger.LogInformation("Telnet server listening on {Address}:{Port}", _endpoint.Address, _endpoint.Port);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);

                if (!TryAcquireSessionSlot())
                {
                    var rejectionTask = RejectClientAsync(client, cancellationToken);
                    RegisterClientTask(rejectionTask);
                    continue;
                }

                var task = Task.Run(() => HandleClientAsync(client, cancellationToken), CancellationToken.None);
                RegisterClientTask(task);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }

        await Task.WhenAll(GetClientTasksSnapshot());

        _logger.LogInformation("Telnet server shut down on {Address}:{Port}", _endpoint.Address, _endpoint.Port);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping telnet server on {Address}:{Port}", _endpoint.Address, _endpoint.Port);
        _listener?.Stop();
        _listener = null;

        await Task.WhenAll(GetClientTasksSnapshot());
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var remoteEndPoint = TryGetRemoteEndPoint(client, out var remoteDisplay);
        var remote = remoteDisplay ?? "unknown";

        var sessionActivity = ActivitySource.StartActivity("telnet.session", ActivityKind.Server);
        var sessionContext = sessionActivity?.Context ?? default;

        if (sessionActivity is not null)
        {
            sessionActivity.SetTag("net.transport", "ip_tcp");
            sessionActivity.SetTag("net.host.ip", _endpoint.Address.ToString());
            sessionActivity.SetTag("net.host.port", _endpoint.Port);

            if (remoteEndPoint is not null)
            {
                sessionActivity.SetTag("net.peer.ip", remoteEndPoint.Address.ToString());
                sessionActivity.SetTag("net.peer.port", remoteEndPoint.Port);
            }
            else if (!string.Equals(remote, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                sessionActivity.SetTag("net.peer.name", remote);
            }
        }

        _logger.LogInformation("Accepted session from {Remote}", remote);
        _metrics.RecordAccepted();

        var sessionEndStatus = ActivityStatusCode.Ok;
        string? sessionEndDescription = null;

        CancellationTokenSource? idleCts = null;
        CancellationTokenSource? heartbeatCts = null;
        CancellationTokenSource? lifetimeCts = null;
        Task? lifetimeTask = null;

        try
        {
            using var connection = client;
            connection.NoDelay = true;
            using var stream = connection.GetStream();
            using var registration = cancellationToken.Register(() => connection.Close());
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true)
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };

            var session = new GameSession(GameRuntime.CreateDefault());

            await writer.WriteLineAsync("歡迎來到《金瓶梅》互動敘事實驗。");
            await writer.WriteLineAsync(" Type 'help' for prototype commands. Type 'quit' to disconnect.");
            await writer.WriteLineAsync(string.Empty);
            await writer.WriteLineAsync(session.RenderIntro());

            if (sessionActivity is not null)
            {
                sessionActivity.AddEvent(new ActivityEvent("session_ready"));
                sessionActivity.SetStatus(ActivityStatusCode.Ok);
                sessionActivity.Dispose();
                sessionActivity = null;
            }

            if (_options.SessionLifetime > TimeSpan.Zero)
            {
                lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                lifetimeTask = Task.Delay(_options.SessionLifetime, lifetimeCts.Token);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await writer.WriteAsync("> ");

                idleCts?.Dispose();
                idleCts = _options.SessionIdleTimeout > TimeSpan.Zero
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;
                var idleTask = idleCts is null ? null : Task.Delay(_options.SessionIdleTimeout, idleCts.Token);

                heartbeatCts?.Dispose();
                heartbeatCts = _options.HeartbeatInterval > TimeSpan.Zero
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;
                var heartbeatTask = heartbeatCts is null ? null : Task.Delay(_options.HeartbeatInterval, heartbeatCts.Token);

                var readTask = reader.ReadLineAsync();

                while (true)
                {
                    var candidates = BuildAwaitables(readTask, idleTask, heartbeatTask, lifetimeTask);
                    var completed = await Task.WhenAny(candidates);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (completed == readTask)
                    {
                        var line = await readTask;
                        idleCts?.Cancel();
                        heartbeatCts?.Cancel();

                        if (line is null)
                        {
                            return;
                        }

                        var trimmed = line.Trim();

                        if (trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        {
                            using var exitActivity = StartChildActivity("telnet.session.client_exit", sessionContext);
                            exitActivity?.SetTag("telnet.command", trimmed);
                            exitActivity?.SetStatus(ActivityStatusCode.Ok);
                            await writer.WriteLineAsync("再見，期待下次相會。");
                            return;
                        }

                        using var commandActivity = StartChildActivity("telnet.command", sessionContext);
                        if (commandActivity is not null)
                        {
                            commandActivity.SetTag("telnet.command", trimmed);
                        }

                        var response = session.HandleCommand(trimmed);
                        var responseLength = response?.Length ?? 0;
                        commandActivity?.SetTag("telnet.response.length", responseLength);
                        commandActivity?.AddEvent(new ActivityEvent("command_processed", tags: new ActivityTagsCollection
                        {
                            { "telnet.command", trimmed },
                            { "telnet.response.length", responseLength }
                        }));
                        await writer.WriteLineAsync(response);
                        break;
                    }

                    if (idleTask is not null && completed == idleTask)
                    {
                        if (idleTask.IsCanceled)
                        {
                            continue;
                        }

                        _metrics.RecordInactivityTimeout();
                        using (var idleTimeoutActivity = StartChildActivity("telnet.session.idle_timeout", sessionContext))
                        {
                            idleTimeoutActivity?.SetStatus(ActivityStatusCode.Error, "Session idle timeout");
                        }
                        _logger.LogInformation("Session from {Remote} ended due to inactivity timeout", remote);
                        sessionEndStatus = ActivityStatusCode.Error;
                        sessionEndDescription = "Session ended due to inactivity timeout";
                        await writer.WriteLineAsync(_options.InactivityMessage);
                        return;
                    }

                    if (heartbeatTask is not null && completed == heartbeatTask)
                    {
                        if (heartbeatTask.IsCanceled)
                        {
                            continue;
                        }

                        await writer.WriteLineAsync(_options.HeartbeatMessage);

                        heartbeatCts?.Dispose();
                        if (_options.HeartbeatInterval > TimeSpan.Zero)
                        {
                            heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            heartbeatTask = Task.Delay(_options.HeartbeatInterval, heartbeatCts.Token);
                        }

                        continue;
                    }

                    if (lifetimeTask is not null && completed == lifetimeTask)
                    {
                        if (lifetimeTask.IsCanceled)
                        {
                            continue;
                        }

                        _metrics.RecordLifetimeLimit();
                        using (var lifetimeActivity = StartChildActivity("telnet.session.lifetime_limit", sessionContext))
                        {
                            lifetimeActivity?.SetStatus(ActivityStatusCode.Error, "Session lifetime limit reached");
                        }
                        _logger.LogInformation("Session from {Remote} reached maximum lifetime", remote);
                        sessionEndStatus = ActivityStatusCode.Error;
                        sessionEndDescription = "Session reached maximum lifetime";
                        await writer.WriteLineAsync(_options.LifetimeExceededMessage);
                        return;
                    }
                }
            }
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Session from {Remote} closed due to I/O error", remote);
            sessionEndStatus = ActivityStatusCode.Error;
            sessionEndDescription = $"I/O error: {ex.Message}";
            if (sessionActivity is not null)
            {
                sessionActivity.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
            else
            {
                RecordSessionException(sessionContext, ex);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogDebug(ex, "Session from {Remote} closed due to socket error", remote);
            sessionEndStatus = ActivityStatusCode.Error;
            sessionEndDescription = $"Socket error: {ex.Message}";
            if (sessionActivity is not null)
            {
                sessionActivity.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
            else
            {
                RecordSessionException(sessionContext, ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing session from {Remote}", remote);
            sessionEndStatus = ActivityStatusCode.Error;
            sessionEndDescription = ex.Message;
            if (sessionActivity is not null)
            {
                sessionActivity.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
            else
            {
                RecordSessionException(sessionContext, ex);
            }
        }
        finally
        {
            idleCts?.Dispose();
            heartbeatCts?.Dispose();
            lifetimeCts?.Dispose();
            ReleaseSessionSlot();
            _logger.LogInformation("Session ended from {Remote}", remote);
            sessionActivity?.Dispose();

            using var endActivity = StartChildActivity("telnet.session.end", sessionContext);
            if (endActivity is not null)
            {
                endActivity.SetStatus(sessionEndStatus, sessionEndDescription);
                endActivity.SetTag("telnet.session.remote", remote);
            }
        }
    }

    private void RegisterClientTask(Task task)
    {
        lock (_sync)
        {
            _clientTasks.Add(task);
        }

        task.ContinueWith(_ =>
        {
            lock (_sync)
            {
                _clientTasks.Remove(task);
            }
        }, TaskScheduler.Default);
    }

    private Task[] GetClientTasksSnapshot()
    {
        lock (_sync)
        {
            return _clientTasks.ToArray();
        }
    }

    private bool TryAcquireSessionSlot()
    {
        return _sessionSlots.Wait(0);
    }

    private void ReleaseSessionSlot()
    {
        _sessionSlots.Release();
    }

    private async Task RejectClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var connection = client;
        connection.NoDelay = true;
        var remoteEndPoint = TryGetRemoteEndPoint(client, out var remoteDisplay);
        var remote = remoteDisplay ?? "unknown";

        using var rejectionActivity = ActivitySource.StartActivity("telnet.session.reject", ActivityKind.Server);
        if (rejectionActivity is not null)
        {
            rejectionActivity.SetTag("net.transport", "ip_tcp");
            rejectionActivity.SetTag("net.host.ip", _endpoint.Address.ToString());
            rejectionActivity.SetTag("net.host.port", _endpoint.Port);

            if (remoteEndPoint is not null)
            {
                rejectionActivity.SetTag("net.peer.ip", remoteEndPoint.Address.ToString());
                rejectionActivity.SetTag("net.peer.port", remoteEndPoint.Port);
            }
            else if (!string.Equals(remote, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                rejectionActivity.SetTag("net.peer.name", remote);
            }

            rejectionActivity.SetStatus(ActivityStatusCode.Error, "Max concurrent sessions reached");
        }

        _metrics.RecordRejected();
        _logger.LogWarning("Rejecting session from {Remote} because capacity {Capacity} reached", remote, _options.MaxConcurrentSessions);

        try
        {
            using var stream = connection.GetStream();
            var message = _options.BusyMessage + "\r\n";
            var buffer = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(buffer, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancellation occurs during shutdown; no further action needed.
        }
        catch (IOException)
        {
            // Client disconnected before the rejection message completed; ignore.
        }
        catch (ObjectDisposedException)
        {
            // Connection already disposed; ignore.
        }
        catch (SocketException)
        {
            // Socket closed while writing the rejection; ignore.
        }
    }

    private static Task[] BuildAwaitables(Task<string?> readTask, Task? idleTask, Task? heartbeatTask, Task? lifetimeTask)
    {
        var tasks = new List<Task> { readTask };

        if (idleTask is not null)
        {
            tasks.Add(idleTask);
        }

        if (heartbeatTask is not null)
        {
            tasks.Add(heartbeatTask);
        }

        if (lifetimeTask is not null)
        {
            tasks.Add(lifetimeTask);
        }

        return tasks.ToArray();
    }

    private static Activity? StartChildActivity(string name, ActivityContext parentContext, ActivityKind kind = ActivityKind.Internal)
    {
        return parentContext != default
            ? ActivitySource.StartActivity(name, kind, parentContext)
            : ActivitySource.StartActivity(name, kind);
    }

    private static void RecordSessionException(ActivityContext sessionContext, Exception exception)
    {
        using var activity = StartChildActivity("telnet.session.exception", sessionContext);
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName ?? exception.GetType().Name },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace ?? string.Empty }
        }));
    }

    private static IPEndPoint? TryGetRemoteEndPoint(TcpClient client, out string? display)
    {
        display = null;
        try
        {
            var endpoint = client.Client.RemoteEndPoint;
            display = endpoint?.ToString();
            return endpoint as IPEndPoint;
        }
        catch (ObjectDisposedException)
        {
            display = null;
            return null;
        }
    }

    private sealed class NullTelnetServerMetrics : ITelnetServerMetrics
    {
        public void RecordAccepted()
        {
        }

        public void RecordRejected()
        {
        }

        public void RecordInactivityTimeout()
        {
        }

        public void RecordLifetimeLimit()
        {
        }
    }
}
