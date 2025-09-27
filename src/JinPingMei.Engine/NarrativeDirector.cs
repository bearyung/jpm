using System;
using System.Collections.Generic;
using System.Linq;
using JinPingMei.AI;
using JinPingMei.Content;

namespace JinPingMei.Engine;

public sealed class NarrativeDirector
{
    private readonly AiOrchestrator _orchestrator;
    private readonly LoreRepository _loreRepository;

    public NarrativeDirector(AiOrchestrator orchestrator, LoreRepository loreRepository)
    {
        _orchestrator = orchestrator;
        _loreRepository = loreRepository;
    }

    public IntroSequence BuildIntroductoryScene()
    {
        var locale = _loreRepository.GetOpeningLocaleDefinition();
        var defaultSceneName = locale.Scenes
            .FirstOrDefault(scene => string.Equals(scene.Id, locale.DefaultSceneId, StringComparison.OrdinalIgnoreCase))?.Name
            ?? locale.Name;

        var narrativeSteps = _loreRepository.GetOpeningNarrativeSteps(locale.Id);

        var placeholders = new Dictionary<string, string>
        {
            ["{{localeName}}"] = locale.Name,
            ["{{localeSummary}}"] = locale.Summary,
            ["{{entrySceneName}}"] = defaultSceneName
        };

        var sequence = _orchestrator.ComposeIntroductorySequence(narrativeSteps, placeholders);

        if (sequence.Steps.Count > 0)
        {
            return sequence;
        }

        var fallbackStep = new[]
        {
            string.Format("你抵達了{0}，故事即將展開。", locale.Name)
        };

        return new IntroSequence(new IReadOnlyList<string>[] { fallbackStep });
    }
}
