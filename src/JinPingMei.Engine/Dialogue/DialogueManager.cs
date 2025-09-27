using System;
using System.Collections.Generic;
using System.Linq;
using JinPingMei.Content.Dialogue;

namespace JinPingMei.Engine.Dialogue;

public sealed class DialogueManager
{
    private readonly DialogueDefinition _definition;
    private readonly DialogueState _state;
    private readonly Dictionary<string, List<DialogueSet>> _dialogueByNpc;

    public DialogueManager(DialogueDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _state = new DialogueState();
        _dialogueByNpc = new Dictionary<string, List<DialogueSet>>(StringComparer.OrdinalIgnoreCase);

        // Index dialogues by NPC ID for quick lookup
        foreach (var dialogueSet in definition.DialogueSets)
        {
            if (!_dialogueByNpc.TryGetValue(dialogueSet.NpcId, out var list))
            {
                list = new List<DialogueSet>();
                _dialogueByNpc[dialogueSet.NpcId] = list;
            }
            list.Add(dialogueSet);
        }
    }

    public DialogueState State => _state;

    public DialogueResult? GetDialogue(string npcId, DialogueContext context)
    {
        if (!_dialogueByNpc.TryGetValue(npcId, out var dialogueSets))
        {
            return null;
        }

        // Find dialogue sets that match the current locale and scene
        var matchingSets = dialogueSets.Where(set =>
            (string.IsNullOrEmpty(set.LocaleId) || string.Equals(set.LocaleId, context.CurrentLocaleId, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(set.SceneId) || string.Equals(set.SceneId, context.CurrentSceneId, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        if (matchingSets.Count == 0)
        {
            return null;
        }

        // Find the best matching dialogue entry
        DialogueEntry? bestEntry = null;
        int bestPriority = -1;

        foreach (var dialogueSet in matchingSets)
        {
            foreach (var entry in dialogueSet.Entries)
            {
                if (EvaluateConditions(entry, context, npcId) && entry.Priority > bestPriority)
                {
                    bestEntry = entry;
                    bestPriority = entry.Priority;
                }
            }
        }

        if (bestEntry == null)
        {
            return null;
        }

        // Track this interaction
        _state.RecordInteraction(npcId, bestEntry.Id);

        // Apply effects if any
        if (bestEntry.Effects != null)
        {
            ApplyEffects(bestEntry.Effects, context);
        }

        return new DialogueResult(bestEntry.Lines, bestEntry.Responses, bestEntry.Effects);
    }

    private bool EvaluateConditions(DialogueEntry entry, DialogueContext context, string npcId)
    {
        // Check if this dialogue has been shown too many times
        if (!entry.Repeatable)
        {
            if (_state.HasShownDialogue(npcId, entry.Id))
            {
                return false;
            }
        }

        if (entry.MaxShown.HasValue)
        {
            var shownCount = _state.GetDialogueShownCount(npcId, entry.Id);
            if (shownCount >= entry.MaxShown.Value)
            {
                return false;
            }
        }

        var conditions = entry.Conditions;
        if (conditions == null)
        {
            return true; // No conditions means always valid
        }

        // Check interaction count
        var interactionCount = _state.GetNpcInteractionCount(npcId);
        if (conditions.MinInteractionCount.HasValue && interactionCount < conditions.MinInteractionCount.Value)
        {
            return false;
        }
        if (conditions.MaxInteractionCount.HasValue && interactionCount > conditions.MaxInteractionCount.Value)
        {
            return false;
        }

        // Check required flags
        foreach (var flag in conditions.RequiredFlags)
        {
            if (!_state.HasFlag(flag))
            {
                return false;
            }
        }

        // Check forbidden flags
        foreach (var flag in conditions.ForbiddenFlags)
        {
            if (_state.HasFlag(flag))
            {
                return false;
            }
        }

        // Check host requirement
        if (!string.IsNullOrEmpty(conditions.RequiredHost))
        {
            if (!string.Equals(context.CurrentHost, conditions.RequiredHost, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check items
        foreach (var item in conditions.RequiredItems)
        {
            if (!context.HasItem(item))
            {
                return false;
            }
        }

        // Check time of day
        if (!string.IsNullOrEmpty(conditions.TimeOfDay))
        {
            if (!string.Equals(context.TimeOfDay, conditions.TimeOfDay, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check chapter progress
        if (conditions.ChapterProgress.HasValue)
        {
            if (context.ChapterProgress < conditions.ChapterProgress.Value)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyEffects(DialogueEffects effects, DialogueContext context)
    {
        // Set flags
        foreach (var flag in effects.SetFlags)
        {
            _state.SetFlag(flag);
        }

        // Unset flags
        foreach (var flag in effects.UnsetFlags)
        {
            _state.UnsetFlag(flag);
        }

        // Modify relationships
        foreach (var (npcId, change) in effects.ModifyRelationships)
        {
            _state.ModifyRelationship(npcId, change);
        }

        // Grant items (context should handle this)
        foreach (var item in effects.GrantItems)
        {
            context.GrantItem(item);
        }

        // Trigger events (context should handle this)
        if (!string.IsNullOrEmpty(effects.TriggerEvent))
        {
            context.TriggerEvent(effects.TriggerEvent);
        }
    }
}