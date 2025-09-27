using System;
using JinPingMei.Engine.Story;

namespace JinPingMei.Engine.Story;

public sealed class MissionState
{
    public MissionState(ObjectiveDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));

        // Check if there's an Availability configuration
        if (definition.Availability != null)
        {
            // Honor the explicit DefaultState from Availability
            IsUnlocked = definition.Availability.DefaultState?.ToLowerInvariant() != "locked";
        }
        else
        {
            // Preserve backward compatibility: Optional and Bonus missions are locked by default
            // Story missions and others are unlocked by default
            IsUnlocked = definition.Category?.ToLowerInvariant() switch
            {
                "optional" => false,
                "bonus" => false,
                _ => true
            };
        }
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
