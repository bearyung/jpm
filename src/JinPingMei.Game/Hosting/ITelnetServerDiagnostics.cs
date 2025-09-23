using System;

namespace JinPingMei.Game.Hosting;

public interface ITelnetServerDiagnostics
{
    TelnetServerSnapshot CaptureSnapshot();
}

public readonly record struct TelnetServerSnapshot(
    DateTimeOffset StartedAt,
    TimeSpan Uptime,
    long ActiveSessions,
    long TotalSessions,
    long CompletedSessions,
    long RejectedSessions,
    long SessionErrors,
    long CommandErrors,
    long InactivityTimeouts,
    long LifetimeEnforcements,
    long TotalCommands)
{
    public double SessionsPerMinute => Uptime.TotalMinutes > 0
        ? TotalSessions / Uptime.TotalMinutes
        : TotalSessions;

    public long TotalErrors => SessionErrors + CommandErrors;
}
