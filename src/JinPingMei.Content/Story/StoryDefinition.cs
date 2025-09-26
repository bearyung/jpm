using System;
using System.Collections.Generic;

namespace JinPingMei.Content.Story;

public sealed class StoryDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<VolumeDefinition> Volumes { get; init; } = Array.Empty<VolumeDefinition>();
}

public sealed class VolumeDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int VolumeNumber { get; init; }

    public IReadOnlyList<EpisodeDefinition> Episodes { get; init; } = Array.Empty<EpisodeDefinition>();
}

public sealed class EpisodeDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int EpisodeNumber { get; init; }

    public string StartingSceneId { get; init; } = string.Empty;

    public string? StartingLocaleId { get; init; }

    public IReadOnlyList<string> CompletionSceneIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<CharacterDefinition> AvailableCharacters { get; init; } = Array.Empty<CharacterDefinition>();

    public bool AllowRandomCharacterSelection { get; init; } = true;
}

public sealed class CharacterDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string BackgroundStory { get; init; } = string.Empty;

    public CharacterTraits Traits { get; init; } = new();

    public IReadOnlyDictionary<string, string> CustomAttributes { get; init; } = new Dictionary<string, string>();
}

public sealed class CharacterTraits
{
    public string Personality { get; init; } = string.Empty;

    public string SocialStatus { get; init; } = string.Empty;

    public string Occupation { get; init; } = string.Empty;

    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Relationships { get; init; } = Array.Empty<string>();
}