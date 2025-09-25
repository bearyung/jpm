using System;
using System.Collections.Generic;
using JinPingMei.Game.Localization;
using JinPingMei.Engine.World;

namespace JinPingMei.Game.Hosting.Commands;

public sealed class CommandRouter
{
    private readonly Dictionary<string, ICommandHandler> _handlers;

    public CommandRouter(IEnumerable<ICommandHandler> handlers)
    {
        _handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase);

        foreach (var handler in handlers)
        {
            _handlers[handler.Command] = handler;
        }
    }

    public CommandResult Dispatch(string commandText, CommandContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var trimmed = commandText.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return CommandResult.FromMessage(context.Localize("commands.invalid"));
        }

        var separatorIndex = trimmed.IndexOf(' ');
        var commandName = separatorIndex >= 0 ? trimmed[..separatorIndex] : trimmed;
        var arguments = separatorIndex >= 0 ? trimmed[(separatorIndex + 1)..].Trim() : string.Empty;

        if (_handlers.TryGetValue(commandName, out var handler))
        {
            return handler.Handle(context, arguments);
        }

        return CommandResult.FromMessage(context.Localize("commands.unknown"));
    }

    public static CommandRouter CreateDefault(ILocalizationProvider localization, ITelnetServerDiagnostics diagnostics, IEnumerable<ICommandHandler>? additionalHandlers = null)
    {
        _ = localization ?? throw new ArgumentNullException(nameof(localization));
        _ = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        var handlers = new List<ICommandHandler>
        {
            new HelpCommandHandler(),
            new NameCommandHandler(),
            new LookCommandHandler(),
            new GoCommandHandler(),
            new ExamineCommandHandler(),
            new SayCommandHandler(),
            new QuitCommandHandler(),
            new DiagnosticsCommandHandler(diagnostics),
            new HealthCommandHandler(diagnostics)
        };

        if (additionalHandlers is not null)
        {
            foreach (var handler in additionalHandlers)
            {
                handlers.Add(handler);
            }
        }

        return new CommandRouter(handlers);
    }
}

public sealed record CommandResult(IReadOnlyList<string> Lines, bool ShouldDisconnect)
{
    public static CommandResult Empty { get; } = new(Array.Empty<string>(), false);

    public static CommandResult FromMessage(string message, bool shouldDisconnect = false)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new CommandResult(Array.Empty<string>(), shouldDisconnect);
        }

        return new CommandResult(new[] { message }, shouldDisconnect);
    }
}

public sealed class CommandContext
{
    private readonly ILocalizationProvider _localization;
    private readonly ITelnetServerDiagnostics _diagnostics;

    public CommandContext(SessionState session, WorldSession world, JinPingMei.Engine.Story.StorySession? story, ILocalizationProvider localization, ITelnetServerDiagnostics diagnostics)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Story = story;
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public SessionState Session { get; }

    public WorldSession World { get; }

    public JinPingMei.Engine.Story.StorySession? Story { get; }

    public ITelnetServerDiagnostics Diagnostics => _diagnostics;

    public string Localize(string key)
    {
        return _localization.GetString(Session.Locale, key);
    }

    public string Format(string key, params object[] arguments)
    {
        var value = Localize(key);
        return string.Format(value, arguments);
    }
}

public interface ICommandHandler
{
    string Command { get; }

    CommandResult Handle(CommandContext context, string arguments);
}
