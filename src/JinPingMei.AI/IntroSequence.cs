using System;
using System.Collections.Generic;

namespace JinPingMei.AI;

public sealed class IntroSequence
{
    public static IntroSequence Empty { get; } = new IntroSequence(Array.Empty<IReadOnlyList<string>>());

    public IntroSequence(IReadOnlyList<IReadOnlyList<string>> steps)
    {
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    public IReadOnlyList<IReadOnlyList<string>> Steps { get; }
}
