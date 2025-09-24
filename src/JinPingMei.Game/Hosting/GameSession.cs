using System;
using System.Collections.Generic;
using JinPingMei.Engine;
using JinPingMei.Engine.World;
using JinPingMei.Game.Hosting.Commands;
using JinPingMei.Game.Localization;

namespace JinPingMei.Game.Hosting;

public sealed class GameSession
{
    private readonly GameRuntime _runtime;
    private readonly SessionState _state;
    private readonly ILocalizationProvider _localization;
    private readonly CommandRouter _commandRouter;
    private readonly CommandContext _commandContext;
    private readonly WorldSession _world;

    public GameSession(GameRuntime runtime, ILocalizationProvider localization, ITelnetServerDiagnostics diagnostics, IEnumerable<ICommandHandler>? additionalHandlers = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _ = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _state = new SessionState { Locale = localization.DefaultLocale };
        _world = runtime.CreateWorldSession();
        _state.CurrentLocaleId = _world.CurrentLocale.Id;
        _state.CurrentSceneId = _world.CurrentScene.Id;
        _commandRouter = CommandRouter.CreateDefault(localization, diagnostics, additionalHandlers);
        _commandContext = new CommandContext(_state, _world, localization, diagnostics);
    }

    public SessionState State => _state;

    public string RenderIntro()
    {
        return _runtime.RenderIntro();
    }

    public string GetCommandHint()
    {
        return _localization.GetString(_state.Locale, "session.commands.hint");
    }

    public CommandResult HandleInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return CommandResult.FromMessage(_commandContext.Localize("story.empty"));
        }

        if (IsCommand(input))
        {
            var commandBody = input[1..];
            return _commandRouter.Dispatch(commandBody, _commandContext);
        }

        return HandleStoryInteraction(input);
    }

    private CommandResult HandleStoryInteraction(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return CommandResult.FromMessage(_commandContext.Localize("story.empty"));
        }

        var displayName = _state.HasPlayerName
            ? _state.PlayerName!
            : _commandContext.Localize("session.display_name.default");

        var response = _commandContext.Format("story.placeholder", displayName, trimmed);
        return CommandResult.FromMessage(response);
    }

    private static bool IsCommand(string input)
    {
        return input.Length > 0 && input[0] == '/';
    }
}
