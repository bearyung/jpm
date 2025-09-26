using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly JinPingMei.Engine.Story.StorySession _story;

    public GameSession(GameRuntime runtime, ILocalizationProvider localization, ITelnetServerDiagnostics diagnostics, IEnumerable<ICommandHandler>? additionalHandlers = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _ = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        _state = new SessionState { Locale = localization.DefaultLocale };
        _world = runtime.CreateWorldSession();
        _state.CurrentLocaleId = _world.CurrentLocale.Id;
        _state.CurrentSceneId = _world.CurrentScene.Id;

        _story = runtime.CreateStorySession("volume-01");

        var handlers = BuildStoryCommandHandlers(diagnostics, additionalHandlers);
        _commandRouter = CommandRouter.CreateDefault(localization, diagnostics, handlers);
        _commandContext = new CommandContext(_state, _world, _story, localization, diagnostics);
    }

    public SessionState State => _state;

    public JinPingMei.Engine.Story.StorySession? Story => _story;

    public string GetCurrentLocationDisplayName()
    {
        return $"{_world.CurrentLocale.Name} › {_world.CurrentScene.Name}";
    }

    public string GetCurrentLocationName()
    {
        return _world.CurrentScene.Name;
    }

    public string GetCurrentLocaleName()
    {
        return _world.CurrentLocale.Name;
    }

    public string RenderIntro()
    {
        return _runtime.RenderIntro();
    }

    public string GetCommandHint()
    {
        return _localization.GetString(_state.Locale, "session.commands.hint");
    }

    public SceneSnapshot GetCurrentSceneSnapshot()
    {
        var locale = _world.CurrentLocale;
        var scene = _world.CurrentScene;

        var npcs = scene.Npcs.Select(npc => npc.Name).ToList();
        var exits = scene.Exits.Select(exit => new SceneExitSnapshot(exit.DisplayName, exit.Description)).ToList();

        return new SceneSnapshot(
            locale.Name,
            locale.Summary,
            scene.Name,
            scene.Description,
            npcs,
            exits);
    }

    public CommandResult HandleInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return HandleStoryInteraction();
        }

        if (IsCommand(input))
        {
            var commandBody = input[1..];
            return _commandRouter.Dispatch(commandBody, _commandContext);
        }

        return HandleStoryInteraction();
    }

    private CommandResult HandleStoryInteraction()
    {
        if (!_state.HasStoryHost)
        {
            return CommandResult.FromMessage("請先使用 /host <角色名稱> 選擇宿主。\n");
        }

        var result = _story.Advance();
        if (result.Messages.Count == 0)
        {
            return CommandResult.Empty;
        }

        return new CommandResult(result.Messages, result.StoryCompleted);
    }

    private static IEnumerable<ICommandHandler> BuildStoryCommandHandlers(ITelnetServerDiagnostics diagnostics, IEnumerable<ICommandHandler>? additional)
    {
        var handlers = new List<ICommandHandler>
        {
            new HostCommandHandler(),
            new StoryStatusCommandHandler(),
            new ProgressCommandHandler()
        };

        if (additional is not null)
        {
            handlers.AddRange(additional);
        }

        return handlers;
    }

    private static bool IsCommand(string input)
    {
        return input.Length > 0 && input[0] == '/';
    }
}

public sealed record SceneSnapshot(
    string LocaleName,
    string LocaleSummary,
    string SceneName,
    string SceneDescription,
    IReadOnlyList<string> NpcNames,
    IReadOnlyList<SceneExitSnapshot> Exits);

public sealed record SceneExitSnapshot(string DisplayName, string Description);