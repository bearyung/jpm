using System;
using System.Collections.Generic;

namespace JinPingMei.Engine.Story;

public sealed class StoryState
{
    private readonly Dictionary<string, object> _flags = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _inventory = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _reputation = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _relationships = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unlockedLore = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unlockedSideQuests = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unlockedMissions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unlockedRecipes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _statuses = new(StringComparer.OrdinalIgnoreCase);

    public StoryState(string volumeId)
    {
        VolumeId = volumeId ?? throw new ArgumentNullException(nameof(volumeId));
    }

    public string VolumeId { get; }

    public string? HostId { get; set; }

    public IReadOnlyDictionary<string, object> Flags => _flags;

    public IReadOnlyDictionary<string, HashSet<string>> Inventory => _inventory;

    public IReadOnlyDictionary<string, int> Reputation => _reputation;

    public IReadOnlyDictionary<string, int> Relationships => _relationships;

    public IReadOnlyCollection<string> UnlockedLore => _unlockedLore;

    public IReadOnlyCollection<string> UnlockedSideQuests => _unlockedSideQuests;

    public IReadOnlyCollection<string> UnlockedMissions => _unlockedMissions;

    public IReadOnlyCollection<string> UnlockedRecipes => _unlockedRecipes;

    public IReadOnlyCollection<string> Statuses => _statuses;

    public bool GetFlag(string id)
    {
        if (!_flags.TryGetValue(id, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool boolValue => boolValue,
            string strValue => bool.TryParse(strValue, out var parsed) && parsed,
            int intValue => intValue != 0,
            double doubleValue => Math.Abs(doubleValue) > double.Epsilon,
            _ => false
        };
    }

    public object? GetFlagValue(string id)
    {
        return _flags.TryGetValue(id, out var value) ? value : null;
    }

    public void SetFlag(string id, object value)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        _flags[id] = value;
    }

    public void AddInventoryItem(string location, string itemId)
    {
        if (string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        if (!_inventory.TryGetValue(location, out var items))
        {
            items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _inventory[location] = items;
        }

        items.Add(itemId);
    }

    public bool TryGetInventoryItems(string location, out HashSet<string> items)
    {
        return _inventory.TryGetValue(location, out items!);
    }

    public void GrantStatus(string statusId)
    {
        if (!string.IsNullOrWhiteSpace(statusId))
        {
            _statuses.Add(statusId);
        }
    }

    public void ModifyReputation(string factionId, int delta)
    {
        if (string.IsNullOrWhiteSpace(factionId))
        {
            return;
        }

        _reputation.TryGetValue(factionId, out var current);
        _reputation[factionId] = current + delta;
    }

    public void ModifyRelationship(string targetId, int delta)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return;
        }

        _relationships.TryGetValue(targetId, out var current);
        _relationships[targetId] = current + delta;
    }

    public void UnlockLore(string loreId)
    {
        if (!string.IsNullOrWhiteSpace(loreId))
        {
            _unlockedLore.Add(loreId);
        }
    }

    public void UnlockSideQuest(string questId)
    {
        if (!string.IsNullOrWhiteSpace(questId))
        {
            _unlockedSideQuests.Add(questId);
        }
    }

    public void UnlockMission(string missionId)
    {
        if (!string.IsNullOrWhiteSpace(missionId))
        {
            _unlockedMissions.Add(missionId);
        }
    }

    public void UnlockRecipe(string recipeId)
    {
        if (!string.IsNullOrWhiteSpace(recipeId))
        {
            _unlockedRecipes.Add(recipeId);
        }
    }
}
