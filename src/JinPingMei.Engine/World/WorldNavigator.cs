using System;
using System.Collections.Generic;
using System.Linq;
using JinPingMei.Content.World;

namespace JinPingMei.Engine.World;

public sealed class WorldNavigator
{
    private readonly WorldDefinition _definition;
    private readonly Dictionary<string, LocaleDefinition> _localesById;
    private readonly Dictionary<string, (LocaleDefinition Locale, SceneDefinition Scene)> _scenesById;

    public WorldNavigator(WorldDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _localesById = definition.Locales.ToDictionary(l => l.Id, StringComparer.OrdinalIgnoreCase);
        _scenesById = new Dictionary<string, (LocaleDefinition, SceneDefinition)>(StringComparer.OrdinalIgnoreCase);

        foreach (var locale in definition.Locales)
        {
            foreach (var scene in locale.Scenes)
            {
                if (!_scenesById.TryAdd(scene.Id, (locale, scene)))
                {
                    throw new InvalidOperationException($"Duplicate scene id '{scene.Id}' detected while building world index.");
                }
            }
        }
    }

    public WorldSession CreateSession()
    {
        var localeId = string.IsNullOrWhiteSpace(_definition.DefaultLocaleId)
            ? _definition.Locales.First().Id
            : _definition.DefaultLocaleId;

        var locale = GetLocale(localeId);
        var sceneId = string.IsNullOrWhiteSpace(locale.DefaultSceneId)
            ? locale.Scenes.First().Id
            : locale.DefaultSceneId;

        var entry = GetSceneEntry(sceneId, locale.Id);
        return new WorldSession(this, entry.Locale, entry.Scene);
    }

    internal LocaleDefinition GetLocale(string localeId)
    {
        if (string.IsNullOrWhiteSpace(localeId))
        {
            throw new ArgumentException("Locale id cannot be empty.", nameof(localeId));
        }

        if (!_localesById.TryGetValue(localeId, out var locale))
        {
            throw new InvalidOperationException($"Locale '{localeId}' was not found in world definition.");
        }

        return locale;
    }

    internal (LocaleDefinition Locale, SceneDefinition Scene) GetSceneEntry(string sceneId, string? expectedLocaleId = null)
    {
        if (!_scenesById.TryGetValue(sceneId, out var entry))
        {
            throw new InvalidOperationException($"Scene '{sceneId}' was not found in world definition.");
        }

        if (!string.IsNullOrWhiteSpace(expectedLocaleId) &&
            !string.Equals(entry.Locale.Id, expectedLocaleId, StringComparison.OrdinalIgnoreCase))
        {
            var locale = GetLocale(expectedLocaleId);
            var scene = locale.Scenes.FirstOrDefault(s => string.Equals(s.Id, sceneId, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException($"Scene '{sceneId}' could not be found in locale '{expectedLocaleId}'.");

            entry = (locale, scene);
        }

        return entry;
    }

    internal bool TryResolveExit(SceneDefinition currentScene, string destination, out LocaleDefinition targetLocale, out SceneDefinition targetScene, out SceneExitDefinition exit)
    {
        exit = null!;
        targetLocale = null!;
        targetScene = null!;

        var normalized = Normalize(destination);
        if (normalized.Length == 0)
        {
            return false;
        }

        foreach (var candidate in currentScene.Exits)
        {
            var entry = GetSceneEntry(candidate.TargetSceneId, candidate.TargetLocaleId);

            if (Matches(entry.Scene, candidate, normalized))
            {
                exit = candidate;
                targetLocale = entry.Locale;
                targetScene = entry.Scene;
                return true;
            }
        }

        return false;
    }

    internal bool TryFindNpc(SceneDefinition scene, string query, out NpcDefinition npc)
    {
        var normalized = Normalize(query);
        if (normalized.Length == 0)
        {
            npc = null!;
            return false;
        }

        foreach (var candidate in scene.Npcs)
        {
            if (candidate.Aliases.Any(alias => string.Equals(alias, normalized, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(candidate.Name, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.Id, normalized, StringComparison.OrdinalIgnoreCase))
            {
                npc = candidate;
                return true;
            }
        }

        npc = null!;
        return false;
    }

    private static string Normalize(string text)
    {
        return text?.Trim() ?? string.Empty;
    }

    private static bool Matches(SceneDefinition scene, SceneExitDefinition exit, string normalizedQuery)
    {
        if (string.Equals(scene.Id, normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scene.Name, normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(exit.DisplayName, normalizedQuery, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return exit.Aliases.Any(alias => string.Equals(alias, normalizedQuery, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class WorldSession
{
    private readonly WorldNavigator _navigator;

    internal WorldSession(WorldNavigator navigator, LocaleDefinition locale, SceneDefinition scene)
    {
        _navigator = navigator;
        CurrentLocale = locale;
        CurrentScene = scene;
    }

    public LocaleDefinition CurrentLocale { get; private set; }

    public SceneDefinition CurrentScene { get; private set; }

    public IReadOnlyList<SceneExitDefinition> Exits => CurrentScene.Exits;

    public IReadOnlyList<NpcDefinition> Npcs => CurrentScene.Npcs;

    public bool TryMove(string destination, out SceneExitDefinition exit)
    {
        if (_navigator.TryResolveExit(CurrentScene, destination, out var locale, out var scene, out exit))
        {
            CurrentLocale = locale;
            CurrentScene = scene;
            return true;
        }

        exit = null!;
        return false;
    }

    public bool TryFindNpc(string query, out NpcDefinition npc)
    {
        return _navigator.TryFindNpc(CurrentScene, query, out npc);
    }
}
