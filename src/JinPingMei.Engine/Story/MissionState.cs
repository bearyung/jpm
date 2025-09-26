using System;
using JinPingMei.Engine.Story;

namespace JinPingMei.Engine.Story;

public sealed class MissionState
{
    public MissionState(ObjectiveDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public ObjectiveDefinition Definition { get; }

    public bool IsCompleted { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

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
