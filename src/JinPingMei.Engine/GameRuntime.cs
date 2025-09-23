using JinPingMei.AI;
using JinPingMei.Content;

namespace JinPingMei.Engine;

public sealed class GameRuntime
{
    private readonly NarrativeDirector _director;

    private GameRuntime(NarrativeDirector director)
    {
        _director = director;
    }

    public static GameRuntime CreateDefault()
    {
        var lore = new LoreRepository();
        var orchestrator = new AiOrchestrator();
        var director = new NarrativeDirector(orchestrator, lore);
        return new GameRuntime(director);
    }

    public string RenderIntro()
    {
        return _director.BuildIntroductoryScene();
    }
}
