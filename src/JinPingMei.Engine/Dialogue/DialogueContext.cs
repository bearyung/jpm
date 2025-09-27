using System;
using System.Collections.Generic;
using JinPingMei.Content.Dialogue;

namespace JinPingMei.Engine.Dialogue;

public sealed class DialogueContext
{
    private readonly HashSet<string> _inventory;
    private readonly Action<string>? _itemGrantCallback;
    private readonly Action<string>? _eventTriggerCallback;

    public DialogueContext(
        string currentLocaleId,
        string currentSceneId,
        string? currentHost = null,
        IEnumerable<string>? inventory = null,
        Action<string>? itemGrantCallback = null,
        Action<string>? eventTriggerCallback = null)
    {
        CurrentLocaleId = currentLocaleId;
        CurrentSceneId = currentSceneId;
        CurrentHost = currentHost;
        _inventory = inventory != null ? new HashSet<string>(inventory, StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _itemGrantCallback = itemGrantCallback;
        _eventTriggerCallback = eventTriggerCallback;

        // Default values
        TimeOfDay = "day";
        ChapterProgress = 0;
    }

    public string CurrentLocaleId { get; }
    public string CurrentSceneId { get; }
    public string? CurrentHost { get; }
    public string TimeOfDay { get; set; }
    public int ChapterProgress { get; set; }

    public bool HasItem(string itemId)
    {
        return _inventory.Contains(itemId);
    }

    public void GrantItem(string itemId)
    {
        _inventory.Add(itemId);
        _itemGrantCallback?.Invoke(itemId);
    }

    public void TriggerEvent(string eventId)
    {
        _eventTriggerCallback?.Invoke(eventId);
    }
}

public sealed class DialogueResult
{
    public DialogueResult(
        IReadOnlyList<string> lines,
        IReadOnlyList<DialogueResponse> responses,
        DialogueEffects? effects)
    {
        Lines = lines;
        Responses = responses;
        Effects = effects;
    }

    public IReadOnlyList<string> Lines { get; }
    public IReadOnlyList<DialogueResponse> Responses { get; }
    public DialogueEffects? Effects { get; }
}