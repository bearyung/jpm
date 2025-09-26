using System;
using JinPingMei.Engine.Story;

namespace JinPingMei.Engine.Story;

public sealed class MissionState
{
    public MissionState(ObjectiveDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        // Main missions are unlocked by default, side quests need to be unlocked
        IsUnlocked = definition.Category?.ToLowerInvariant() switch
        {
            "optional" => false,
            "bonus" => false,
            _ => true
        };
    }

    public ObjectiveDefinition Definition { get; }

    public bool IsUnlocked { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void Unlock()
    {
        IsUnlocked = true;
    }

    public void MarkComplete()
    {
        if (IsCompleted)
        {
            return;
        }

        IsCompleted = true;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
