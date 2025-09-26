using System.Collections.Generic;

namespace JinPingMei.Engine.Story;

public sealed class StoryAdvanceResult
{
    public StoryAdvanceResult(IReadOnlyList<string> messages, bool storyCompleted)
    {
        Messages = messages;
        StoryCompleted = storyCompleted;
    }

    public IReadOnlyList<string> Messages { get; }

    public bool StoryCompleted { get; }

    public static StoryAdvanceResult FromMessage(string message, bool storyCompleted = false)
    {
        return new StoryAdvanceResult(new[] { message }, storyCompleted);
    }
}
