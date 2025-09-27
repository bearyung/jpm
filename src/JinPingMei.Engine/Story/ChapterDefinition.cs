using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JinPingMei.Engine.Story;

public sealed class ChapterDefinition
{
    [JsonPropertyName("Id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("Number")]
    public int Number { get; init; }

    [JsonPropertyName("Titles")]
    public IReadOnlyList<string> Titles { get; init; } = Array.Empty<string>();

    [JsonPropertyName("Text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("Gameplay")]
    public GameplayDefinition Gameplay { get; init; } = new();
}

public sealed class GameplayDefinition
{
    [JsonPropertyName("Synopsis")]
    public string Synopsis { get; init; } = string.Empty;

    [JsonPropertyName("PrimaryCharacters")]
    public IReadOnlyList<CharacterProfile> PrimaryCharacters { get; init; } = Array.Empty<CharacterProfile>();

    [JsonPropertyName("SupportingCharacters")]
    public IReadOnlyList<CharacterProfile> SupportingCharacters { get; init; } = Array.Empty<CharacterProfile>();

    [JsonPropertyName("Locations")]
    public IReadOnlyList<LocationProfile> Locations { get; init; } = Array.Empty<LocationProfile>();

    [JsonPropertyName("EntryState")]
    public string EntryState { get; init; } = string.Empty;

    [JsonPropertyName("ExitState")]
    public string ExitState { get; init; } = string.Empty;

    [JsonPropertyName("Objectives")]
    public IReadOnlyList<ObjectiveDefinition> Objectives { get; init; } = Array.Empty<ObjectiveDefinition>();

    [JsonPropertyName("Scenes")]
    public IReadOnlyList<ChapterSceneDefinition> Scenes { get; init; } = Array.Empty<ChapterSceneDefinition>();

    [JsonPropertyName("EligibleHosts")]
    public IReadOnlyList<string> EligibleHosts { get; init; } = Array.Empty<string>();

    [JsonPropertyName("RecommendedMode")]
    public string? RecommendedMode { get; init; }

    [JsonPropertyName("HostSettings")]
    public IReadOnlyDictionary<string, HostSettingsDefinition> HostSettings { get; init; } = new Dictionary<string, HostSettingsDefinition>(StringComparer.OrdinalIgnoreCase);
}

public sealed class CharacterProfile
{
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("Role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("Intent")]
    public string Intent { get; init; } = string.Empty;
}

public sealed class LocationProfile
{
    [JsonPropertyName("Id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("Notes")]
    public string Notes { get; init; } = string.Empty;
}

public sealed class ObjectiveDefinition
{
    [JsonPropertyName("Id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("Title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("Category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("HostDirectives")]
    public IReadOnlyDictionary<string, string> HostDirectives { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("Completion")]
    public IReadOnlyList<ObjectiveCompletionCondition> Completion { get; init; } = Array.Empty<ObjectiveCompletionCondition>();

    [JsonPropertyName("Availability")]
    public ObjectiveAvailability? Availability { get; init; }
}

public sealed class ObjectiveAvailability
{
    [JsonPropertyName("DefaultState")]
    public string DefaultState { get; init; } = "Unlocked";

    [JsonPropertyName("UnlockConditions")]
    public IReadOnlyList<ObjectiveCompletionCondition> UnlockConditions { get; init; } = Array.Empty<ObjectiveCompletionCondition>();
}

public sealed class ObjectiveCompletionCondition
{
    [JsonPropertyName("Type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("Id")]
    public string? Id { get; init; }

    [JsonPropertyName("Location")]
    public string? Location { get; init; }

    [JsonPropertyName("Comparison")]
    public string? Comparison { get; init; }

    [JsonPropertyName("Value")]
    public JsonElement? Value { get; init; }

    [JsonPropertyName("AllowedValues")]
    public IReadOnlyList<string>? AllowedValues { get; init; }

    [JsonPropertyName("RequiredItems")]
    public IReadOnlyList<string>? RequiredItems { get; init; }

    [JsonPropertyName("MinItems")]
    public int? MinItems { get; init; }

    [JsonPropertyName("MinDistinct")]
    public int? MinDistinct { get; init; }
}

public sealed class ChapterSceneDefinition
{
    [JsonPropertyName("Id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("Title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("Summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("Beats")]
    public IReadOnlyList<SceneBeatDefinition> Beats { get; init; } = Array.Empty<SceneBeatDefinition>();

    [JsonPropertyName("Hooks")]
    public IReadOnlyList<SceneHookDefinition> Hooks { get; init; } = Array.Empty<SceneHookDefinition>();

    [JsonPropertyName("SkillChecks")]
    public IReadOnlyList<SkillCheckDefinition> SkillChecks { get; init; } = Array.Empty<SkillCheckDefinition>();

    [JsonPropertyName("HostsEligible")]
    public IReadOnlyList<string>? HostsEligible { get; init; }

    [JsonPropertyName("HostParticipation")]
    public IReadOnlyDictionary<string, string>? HostParticipation { get; init; }
}

public sealed class SceneBeatDefinition
{
    [JsonPropertyName("Trigger")]
    public string Trigger { get; init; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("EventId")]
    public string? EventId { get; init; }

    [JsonPropertyName("OnSuccessFlags")]
    public IReadOnlyDictionary<string, bool>? OnSuccessFlags { get; init; }
}

public sealed class SceneHookDefinition
{
    [JsonPropertyName("Type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("Payload")]
    public JsonElement? Payload { get; init; }

    [JsonPropertyName("Description")]
    public string Description { get; init; } = string.Empty;
}

public sealed class SkillCheckDefinition
{
    [JsonPropertyName("Host")]
    public string Host { get; init; } = string.Empty;

    [JsonPropertyName("Trait")]
    public string Trait { get; init; } = string.Empty;

    [JsonPropertyName("Difficulty")]
    public string Difficulty { get; init; } = string.Empty;

    [JsonPropertyName("SuccessEvent")]
    public string? SuccessEvent { get; init; }

    [JsonPropertyName("FailureEvent")]
    public string? FailureEvent { get; init; }

    [JsonPropertyName("SuccessFlags")]
    public IReadOnlyDictionary<string, bool>? SuccessFlags { get; init; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; init; }
}

public sealed class HostSettingsDefinition
{
    [JsonPropertyName("HotCapacity")]
    public int? HotCapacity { get; init; }

    [JsonPropertyName("Tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    [JsonPropertyName("StartingTraits")]
    public IReadOnlyList<string> StartingTraits { get; init; } = Array.Empty<string>();

    [JsonPropertyName("Notes")]
    public string? Notes { get; init; }
}
