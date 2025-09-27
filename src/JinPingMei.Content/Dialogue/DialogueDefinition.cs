using System;
using System.Collections.Generic;

namespace JinPingMei.Content.Dialogue;

public sealed class DialogueDefinition
{
    public string DefaultLocaleId { get; init; } = string.Empty;
    public IReadOnlyList<DialogueSet> DialogueSets { get; init; } = Array.Empty<DialogueSet>();
}

public sealed class DialogueSet
{
    public string Id { get; init; } = string.Empty;
    public string NpcId { get; init; } = string.Empty;
    public string LocaleId { get; init; } = string.Empty;
    public string SceneId { get; init; } = string.Empty;
    public IReadOnlyList<DialogueEntry> Entries { get; init; } = Array.Empty<DialogueEntry>();
}

public sealed class DialogueEntry
{
    public string Id { get; init; } = string.Empty;
    public int Priority { get; init; } = 0;
    public DialogueConditions? Conditions { get; init; }
    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
    public IReadOnlyList<DialogueResponse> Responses { get; init; } = Array.Empty<DialogueResponse>();
    public DialogueEffects? Effects { get; init; }
    public bool Repeatable { get; init; } = true;
    public int? MaxShown { get; init; }
}

public sealed class DialogueConditions
{
    public IReadOnlyList<string> RequiredFlags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ForbiddenFlags { get; init; } = Array.Empty<string>();
    public string? RequiredHost { get; init; }
    public int? MinInteractionCount { get; init; }
    public int? MaxInteractionCount { get; init; }
    public IReadOnlyList<string> RequiredItems { get; init; } = Array.Empty<string>();
    public string? TimeOfDay { get; init; }
    public int? ChapterProgress { get; init; }
}

public sealed class DialogueResponse
{
    public string Id { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string? NextDialogueId { get; init; }
    public DialogueEffects? Effects { get; init; }
}

public sealed class DialogueEffects
{
    public IReadOnlyList<string> SetFlags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> UnsetFlags { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, int> ModifyRelationships { get; init; } =
        new Dictionary<string, int>();
    public IReadOnlyList<string> GrantItems { get; init; } = Array.Empty<string>();
    public string? TriggerEvent { get; init; }
}