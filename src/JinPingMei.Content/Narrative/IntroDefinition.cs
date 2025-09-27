using System;
using System.Collections.Generic;

namespace JinPingMei.Content.Narrative;

public sealed class IntroDefinition
{
    public string DefaultLocaleId { get; init; } = string.Empty;

    public IReadOnlyList<IntroLocaleScript> Scripts { get; init; } = Array.Empty<IntroLocaleScript>();
}

public sealed class IntroLocaleScript
{
    public string LocaleId { get; init; } = string.Empty;

    public IReadOnlyList<IntroStepDefinition> Steps { get; init; } = Array.Empty<IntroStepDefinition>();
}

public sealed class IntroStepDefinition
{
    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
}
