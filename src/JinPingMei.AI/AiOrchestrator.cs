using System;
using System.Collections.Generic;
using System.Linq;

namespace JinPingMei.AI;

public sealed class AiOrchestrator
{
    public IntroSequence ComposeIntroductorySequence(
        IReadOnlyList<IReadOnlyList<string>> steps,
        IReadOnlyDictionary<string, string> placeholders)
    {
        if (steps.Count == 0)
        {
            return IntroSequence.Empty;
        }

        var processedSteps = steps
            .Select(step => (IReadOnlyList<string>)step
                .Select(line => ApplyPlaceholders(line, placeholders))
                .ToArray())
            .Where(stepLines => stepLines.Count > 0)
            .ToArray();

        return processedSteps.Length == 0
            ? IntroSequence.Empty
            : new IntroSequence(processedSteps);
    }

    private static string ApplyPlaceholders(string line, IReadOnlyDictionary<string, string> placeholders)
    {
        var result = line;

        foreach (var placeholder in placeholders)
        {
            result = result.Replace(placeholder.Key, placeholder.Value, StringComparison.Ordinal);
        }

        return result;
    }
}
