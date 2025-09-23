using System;
using System.Diagnostics.Tracing;

namespace JinPingMei.Game.Hosting;

[EventSource(Name = "JinPingMei.Game.Hosting.TelnetServer")]
public sealed class TelnetServerEventSource : EventSource
{
    public static TelnetServerEventSource Log { get; } = new();

    private PollingCounter? _activeSessions;
    private EventCounter? _commandLatency;
    private Func<double>? _activeSessionsProvider;
    private bool _initialized;

    private TelnetServerEventSource()
    {
    }

    public void Initialize(Func<double> activeSessionsProvider)
    {
        // EventSource may be reused across tests; guard against duplicate initialization.
        if (_initialized)
        {
            _activeSessionsProvider = activeSessionsProvider;
            return;
        }

        _activeSessionsProvider = activeSessionsProvider;
        _activeSessions = new PollingCounter("active-sessions", this, () => _activeSessionsProvider?.Invoke() ?? 0);
        _commandLatency = new EventCounter("command-latency", this)
        {
            DisplayName = "Command Latency",
            DisplayUnits = "ms"
        };

        _initialized = true;
    }

    public void ReportCommandLatency(TimeSpan elapsed)
    {
        _commandLatency?.WriteMetric((float)elapsed.TotalMilliseconds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activeSessions?.Dispose();
            _commandLatency?.Dispose();
        }

        base.Dispose(disposing);
    }
}
