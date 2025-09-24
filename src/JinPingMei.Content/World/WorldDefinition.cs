using System;
using System.Collections.Generic;

namespace JinPingMei.Content.World;

public sealed class WorldDefinition
{
    public string DefaultLocaleId { get; init; } = string.Empty;

    public IReadOnlyList<LocaleDefinition> Locales { get; init; } = Array.Empty<LocaleDefinition>();
}

public sealed class LocaleDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string DefaultSceneId { get; init; } = string.Empty;

    public IReadOnlyList<SceneDefinition> Scenes { get; init; } = Array.Empty<SceneDefinition>();
}

public sealed class SceneDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<SceneExitDefinition> Exits { get; init; } = Array.Empty<SceneExitDefinition>();

    public IReadOnlyList<NpcDefinition> Npcs { get; init; } = Array.Empty<NpcDefinition>();
}

public sealed class SceneExitDefinition
{
    public string TargetSceneId { get; init; } = string.Empty;

    public string? TargetLocaleId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
}

public sealed class NpcDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
}
