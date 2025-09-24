using JinPingMei.AI;
using JinPingMei.Content;
using JinPingMei.Engine.World;

namespace JinPingMei.Engine;

public sealed class GameRuntime
{
    private readonly NarrativeDirector _director;
    private readonly WorldNavigator _worldNavigator;

    private GameRuntime(NarrativeDirector director, WorldNavigator worldNavigator)
    {
        _director = director;
        _worldNavigator = worldNavigator;
    }

    public static GameRuntime CreateDefault()
    {
        var lore = new LoreRepository();
        var orchestrator = new AiOrchestrator();
        var director = new NarrativeDirector(orchestrator, lore);
        var worldNavigator = new WorldNavigator(lore.LoadWorldDefinition());
        return new GameRuntime(director, worldNavigator);
    }

    public string RenderIntro()
    {
        return _director.BuildIntroductoryScene();
    }

    public WorldSession CreateWorldSession()
    {
        return _worldNavigator.CreateSession();
    }
}
