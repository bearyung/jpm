using System;
using System.Collections.Generic;

namespace JinPingMei.Engine.Dialogue;

public sealed class DialogueState
{
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _npcInteractionCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, int>> _dialogueShownCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _relationships = new(StringComparer.OrdinalIgnoreCase);

    public bool HasFlag(string flag)
    {
        return _flags.Contains(flag);
    }

    public void SetFlag(string flag)
    {
        _flags.Add(flag);
    }

    public void UnsetFlag(string flag)
    {
        _flags.Remove(flag);
    }

    public int GetNpcInteractionCount(string npcId)
    {
        return _npcInteractionCounts.TryGetValue(npcId, out var count) ? count : 0;
    }

    public void RecordInteraction(string npcId, string dialogueId)
    {
        // Increment NPC interaction count
        if (_npcInteractionCounts.TryGetValue(npcId, out var count))
        {
            _npcInteractionCounts[npcId] = count + 1;
        }
        else
        {
            _npcInteractionCounts[npcId] = 1;
        }

        // Track dialogue shown count
        if (!_dialogueShownCounts.TryGetValue(npcId, out var dialogueCounts))
        {
            dialogueCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _dialogueShownCounts[npcId] = dialogueCounts;
        }

        if (dialogueCounts.TryGetValue(dialogueId, out var dialogueCount))
        {
            dialogueCounts[dialogueId] = dialogueCount + 1;
        }
        else
        {
            dialogueCounts[dialogueId] = 1;
        }
    }

    public bool HasShownDialogue(string npcId, string dialogueId)
    {
        return GetDialogueShownCount(npcId, dialogueId) > 0;
    }

    public int GetDialogueShownCount(string npcId, string dialogueId)
    {
        if (_dialogueShownCounts.TryGetValue(npcId, out var dialogueCounts))
        {
            if (dialogueCounts.TryGetValue(dialogueId, out var count))
            {
                return count;
            }
        }
        return 0;
    }

    public int GetRelationship(string npcId)
    {
        return _relationships.TryGetValue(npcId, out var value) ? value : 0;
    }

    public void ModifyRelationship(string npcId, int change)
    {
        if (_relationships.TryGetValue(npcId, out var current))
        {
            _relationships[npcId] = current + change;
        }
        else
        {
            _relationships[npcId] = change;
        }
    }

    public void SetRelationship(string npcId, int value)
    {
        _relationships[npcId] = value;
    }
}