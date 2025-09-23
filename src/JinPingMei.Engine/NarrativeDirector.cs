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

    public string BuildIntroductoryScene()
    {
        var locale = _loreRepository.GetOpeningLocale();
        return _orchestrator.ComposeIntroductoryBeat(locale);
    }
}
