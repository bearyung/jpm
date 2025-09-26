using JinPingMei.Game.Hosting;

namespace JinPingMei.Game;

/// <summary>
/// Null implementation of diagnostics for console mode
/// </summary>
internal sealed class NullDiagnostics : ITelnetServerDiagnostics
{
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    public TelnetServerSnapshot CaptureSnapshot()
    {
        var now = DateTimeOffset.UtcNow;
        return new TelnetServerSnapshot(
            _startedAt,
            now - _startedAt,
            ActiveSessions: 1, // Console mode always has 1 session
            TotalSessions: 1,
            CompletedSessions: 0,
            RejectedSessions: 0,
            SessionErrors: 0,
            CommandErrors: 0,
            InactivityTimeouts: 0,
            LifetimeEnforcements: 0,
            TotalCommands: 0);
    }
}