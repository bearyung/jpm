using System;

namespace JinPingMei.Game.Hosting;

public sealed class TelnetGameServerOptions
{
    public int MaxConcurrentSessions { get; init; } = 100;
    public TimeSpan SessionIdleTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public string BusyMessage { get; init; } = "Server is busy. Please try again later.";
    public string InactivityMessage { get; init; } = "Session timed out due to inactivity.";
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(45);
    public string HeartbeatMessage { get; init; } = "[heartbeat]";
    public TimeSpan SessionLifetime { get; init; } = TimeSpan.FromMinutes(30);
    public string LifetimeExceededMessage { get; init; } = "Session reached the maximum allowed duration.";
}
