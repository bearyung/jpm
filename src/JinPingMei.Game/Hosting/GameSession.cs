using System;
using System.Collections.Generic;
using System.Linq;
using JinPingMei.Content.Story;
using JinPingMei.Engine;
using JinPingMei.Engine.Story;
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
    private readonly StoryProgressTracker _storyTracker;
    private readonly CharacterSelectionHandler _characterSelectionHandler;

    public GameSession(GameRuntime runtime, ILocalizationProvider localization, ITelnetServerDiagnostics diagnostics, IEnumerable<ICommandHandler>? additionalHandlers = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _ = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _state = new SessionState { Locale = localization.DefaultLocale };
        _world = runtime.CreateWorldSession();
        _storyTracker = runtime.GetStoryTracker();
        _characterSelectionHandler = new CharacterSelectionHandler(localization, _state.Locale);
        _state.CurrentLocaleId = _world.CurrentLocale.Id;
        _state.CurrentSceneId = _world.CurrentScene.Id;
        _commandRouter = CommandRouter.CreateDefault(localization, diagnostics, additionalHandlers);
        _commandContext = new CommandContext(_state, _world, localization, diagnostics);

        InitializeFirstEpisode();
    }

    private void InitializeFirstEpisode()
    {
        var firstEpisode = _storyTracker.GetNextEpisode();
        if (firstEpisode != null)
        {
            // For now, we'll use a simple approach with the first volume
            var volumeId = "volume1"; // Default first volume

            if (_storyTracker.TryStartNewEpisode(volumeId, firstEpisode.Id, out _))
            {
                _state.CurrentVolumeId = volumeId;
                _state.CurrentEpisodeId = firstEpisode.Id;
                _state.IsInCharacterSelection = true;
            }
        }
    }

    public SessionState State => _state;

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
        var intro = _runtime.RenderIntro();

        if (_state.IsInCharacterSelection && _storyTracker.CurrentEpisode != null && _storyTracker.CurrentVolume != null)
        {
            intro += "\n" + _characterSelectionHandler.RenderCharacterSelectionPrompt(
                _storyTracker.CurrentEpisode,
                _storyTracker.CurrentVolume);
        }

        return intro;
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
            return CommandResult.FromMessage(_commandContext.Localize("story.empty"));
        }

        if (_state.IsInCharacterSelection)
        {
            return HandleCharacterSelection(input);
        }

        if (IsCommand(input))
        {
            var commandBody = input[1..];
            var result = _commandRouter.Dispatch(commandBody, _commandContext);

            CheckForEpisodeCompletion();

            return result;
        }

        return HandleStoryInteraction(input);
    }

    private CommandResult HandleCharacterSelection(string input)
    {
        if (_storyTracker.CurrentEpisode == null)
        {
            _state.IsInCharacterSelection = false;
            return CommandResult.FromMessage("Error: No episode available for character selection.");
        }

        var selectionResult = _characterSelectionHandler.ParseCharacterSelection(input, _storyTracker.CurrentEpisode);

        if (!selectionResult.IsValid)
        {
            return CommandResult.FromMessage(selectionResult.ErrorMessage ?? "Invalid selection.");
        }

        CharacterDefinition? selectedCharacter = null;

        if (selectionResult.IsRandom)
        {
            selectedCharacter = _storyTracker.SelectRandomCharacter();
            if (selectedCharacter == null)
            {
                return CommandResult.FromMessage("Failed to select random character.");
            }
        }
        else if (selectionResult.SelectedCharacter != null)
        {
            if (_storyTracker.TrySelectCharacter(selectionResult.SelectedCharacter.Id))
            {
                selectedCharacter = selectionResult.SelectedCharacter;
            }
            else
            {
                return CommandResult.FromMessage("Failed to select character.");
            }
        }

        if (selectedCharacter != null)
        {
            _state.CurrentCharacter = selectedCharacter;
            _state.IsInCharacterSelection = false;

            if (!string.IsNullOrWhiteSpace(_storyTracker.CurrentEpisode.StartingSceneId))
            {
                MoveToScene(_storyTracker.CurrentEpisode.StartingSceneId, _storyTracker.CurrentEpisode.StartingLocaleId);
            }

            return CommandResult.FromMessage(
                _characterSelectionHandler.RenderCharacterSelected(selectedCharacter, _storyTracker.CurrentEpisode));
        }

        return CommandResult.FromMessage("Failed to select character.");
    }

    private void MoveToScene(string sceneId, string? localeId)
    {
        // This would need to be implemented in WorldSession to support direct scene navigation
        // For now, we'll just update the state
        _state.CurrentSceneId = sceneId;
        if (!string.IsNullOrWhiteSpace(localeId))
        {
            _state.CurrentLocaleId = localeId;
        }
    }

    private void CheckForEpisodeCompletion()
    {
        if (_state.CurrentSceneId != null &&
            _storyTracker.CurrentEpisode != null &&
            _storyTracker.TryCompleteCurrentEpisode(_state.CurrentSceneId))
        {
            var nextEpisode = _storyTracker.GetNextEpisode();
            if (nextEpisode != null)
            {
                // Prepare for next episode
                _state.CurrentCharacter = null;
                _state.IsInCharacterSelection = true;

                // Find the volume for the next episode
                var story = _runtime.GetStoryTracker();
                foreach (var volume in story.CurrentVolume?.Episodes ?? new List<EpisodeDefinition>())
                {
                    if (volume.Id == nextEpisode.Id && story.TryStartNewEpisode(story.CurrentVolume!.Id, nextEpisode.Id, out _))
                    {
                        _state.CurrentEpisodeId = nextEpisode.Id;
                        break;
                    }
                }
            }
        }
    }

    private CommandResult HandleStoryInteraction(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return CommandResult.FromMessage(_commandContext.Localize("story.empty"));
        }

        var displayName = GetCurrentDisplayName();
        var response = _commandContext.Format("story.placeholder", displayName, trimmed);

        CheckForEpisodeCompletion();

        return CommandResult.FromMessage(response);
    }

    private string GetCurrentDisplayName()
    {
        if (_state.CurrentCharacter != null)
        {
            return _state.CurrentCharacter.Name;
        }

        if (_state.HasPlayerName)
        {
            return _state.PlayerName!;
        }

        return _commandContext.Localize("session.display_name.default");
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
