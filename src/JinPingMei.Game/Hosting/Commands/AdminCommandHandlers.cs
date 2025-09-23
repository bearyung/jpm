using System;
using System.Collections.Generic;

namespace JinPingMei.Game.Hosting.Commands;

public sealed class DiagnosticsCommandHandler : ICommandHandler
{
    private readonly ITelnetServerDiagnostics _diagnostics;

    public DiagnosticsCommandHandler(ITelnetServerDiagnostics diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public string Command => "diagnostics";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        var snapshot = _diagnostics.CaptureSnapshot();
        var uptime = FormatDuration(snapshot.Uptime);

        var lines = new List<string>
        {
            context.Localize("commands.diagnostics.header"),
            context.Format("commands.diagnostics.uptime", uptime),
            context.Format("commands.diagnostics.sessions.active", snapshot.ActiveSessions),
            context.Format("commands.diagnostics.sessions.total", snapshot.TotalSessions, snapshot.RejectedSessions),
            context.Format("commands.diagnostics.sessions.completed", snapshot.CompletedSessions),
            context.Format("commands.diagnostics.errors", snapshot.TotalErrors, snapshot.SessionErrors, snapshot.CommandErrors),
            context.Format("commands.diagnostics.commands", snapshot.TotalCommands, snapshot.SessionsPerMinute)
        };

        return new CommandResult(lines, false);
    }

    private static string FormatDuration(TimeSpan uptime)
    {
        if (uptime.TotalSeconds < 1)
        {
            return "<1 秒";
        }

        if (uptime.TotalMinutes < 1)
        {
            return string.Format("{0:F0} 秒", uptime.TotalSeconds);
        }

        if (uptime.TotalHours < 1)
        {
            return string.Format("{0:F1} 分鐘", uptime.TotalMinutes);
        }

        if (uptime.TotalDays < 1)
        {
            return string.Format("{0:F1} 小時", uptime.TotalHours);
        }

        return string.Format("{0:F1} 天", uptime.TotalDays);
    }
}

public sealed class HealthCommandHandler : ICommandHandler
{
    private readonly ITelnetServerDiagnostics _diagnostics;

    public HealthCommandHandler(ITelnetServerDiagnostics diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public string Command => "health";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        var snapshot = _diagnostics.CaptureSnapshot();
        var severity = snapshot.TotalErrors > 0
            ? context.Format("commands.health.degraded", snapshot.TotalErrors)
            : context.Localize("commands.health.ok");

        var lines = new List<string>
        {
            severity,
            context.Format("commands.health.active", snapshot.ActiveSessions),
            context.Format("commands.health.started", snapshot.StartedAt.ToLocalTime())
        };

        return new CommandResult(lines, false);
    }
}
