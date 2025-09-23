using System.Diagnostics.Metrics;

namespace JinPingMei.Game.Hosting;

public sealed class TelnetServerMetrics : ITelnetServerMetrics, IDisposable
{
    public const string MeterName = "JinPingMei.Game";
    public const string MeterVersion = "1.0";

    private static readonly Meter Meter = new(MeterName, MeterVersion);

    private readonly Counter<long> _sessionsAccepted = Meter.CreateCounter<long>("telnet.sessions.accepted");
    private readonly Counter<long> _sessionsRejected = Meter.CreateCounter<long>("telnet.sessions.rejected");
    private readonly Counter<long> _sessionsInactiveTimeout = Meter.CreateCounter<long>("telnet.sessions.inactive_timeouts");
    private readonly Counter<long> _sessionsLifetimeLimit = Meter.CreateCounter<long>("telnet.sessions.lifetime_ended");
    private bool _disposed;

    public void RecordAccepted() => _sessionsAccepted.Add(1);

    public void RecordRejected() => _sessionsRejected.Add(1);

    public void RecordInactivityTimeout() => _sessionsInactiveTimeout.Add(1);

    public void RecordLifetimeLimit() => _sessionsLifetimeLimit.Add(1);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }
}
