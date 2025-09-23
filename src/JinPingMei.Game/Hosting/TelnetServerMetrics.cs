using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

namespace JinPingMei.Game.Hosting;

public sealed class TelnetServerMetrics : ITelnetServerMetrics, ITelnetServerDiagnostics, IDisposable
{
    public const string MeterName = "JinPingMei.Game";
    public const string MeterVersion = "1.0";

    private static readonly Meter Meter = new(MeterName, MeterVersion);

    private readonly Counter<long> _sessionsAccepted = Meter.CreateCounter<long>("telnet.sessions.accepted");
    private readonly Counter<long> _sessionsRejected = Meter.CreateCounter<long>("telnet.sessions.rejected");
    private readonly Counter<long> _sessionsInactiveTimeout = Meter.CreateCounter<long>("telnet.sessions.inactive_timeouts");
    private readonly Counter<long> _sessionsLifetimeLimit = Meter.CreateCounter<long>("telnet.sessions.lifetime_ended");
    private readonly Histogram<double> _commandLatency = Meter.CreateHistogram<double>("telnet.command.latency", unit: "ms");

    private readonly TelnetServerEventSource _eventSource = TelnetServerEventSource.Log;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    private long _activeSessions;
    private long _totalSessions;
    private long _completedSessions;
    private long _rejectedSessions;
    private long _sessionErrors;
    private long _commandErrors;
    private long _inactivityTimeouts;
    private long _lifetimeEnforcements;
    private long _totalCommands;
    private bool _disposed;

    public TelnetServerMetrics()
    {
        _eventSource.Initialize(() => Volatile.Read(ref _activeSessions));
    }

    public void RecordAccepted()
    {
        _sessionsAccepted.Add(1);
        Interlocked.Increment(ref _activeSessions);
        Interlocked.Increment(ref _totalSessions);
    }

    public void RecordSessionEnded(bool faulted)
    {
        Interlocked.Decrement(ref _activeSessions);
        Interlocked.Increment(ref _completedSessions);

        if (faulted)
        {
            Interlocked.Increment(ref _sessionErrors);
        }
    }

    public void RecordRejected()
    {
        _sessionsRejected.Add(1);
        Interlocked.Increment(ref _rejectedSessions);
    }

    public void RecordInactivityTimeout()
    {
        _sessionsInactiveTimeout.Add(1);
        Interlocked.Increment(ref _inactivityTimeouts);
        Interlocked.Increment(ref _sessionErrors);
    }

    public void RecordLifetimeLimit()
    {
        _sessionsLifetimeLimit.Add(1);
        Interlocked.Increment(ref _lifetimeEnforcements);
        Interlocked.Increment(ref _sessionErrors);
    }

    public void RecordCommand(TimeSpan duration, bool faulted)
    {
        _commandLatency.Record(duration.TotalMilliseconds);
        _eventSource.ReportCommandLatency(duration);

        Interlocked.Increment(ref _totalCommands);

        if (faulted)
        {
            Interlocked.Increment(ref _commandErrors);
        }
    }

    public TelnetServerSnapshot CaptureSnapshot()
    {
        var now = DateTimeOffset.UtcNow;
        var uptime = now - _startedAt;

        return new TelnetServerSnapshot(
            _startedAt,
            uptime,
            Volatile.Read(ref _activeSessions),
            Volatile.Read(ref _totalSessions),
            Volatile.Read(ref _completedSessions),
            Volatile.Read(ref _rejectedSessions),
            Volatile.Read(ref _sessionErrors),
            Volatile.Read(ref _commandErrors),
            Volatile.Read(ref _inactivityTimeouts),
            Volatile.Read(ref _lifetimeEnforcements),
            Volatile.Read(ref _totalCommands));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }
}
