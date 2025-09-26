using JinPingMei.AI;
using JinPingMei.Content;
using JinPingMei.Engine.Story;
using JinPingMei.Engine.World;

namespace JinPingMei.Engine;

public sealed class GameRuntime
{
    private readonly NarrativeDirector _director;
    private readonly WorldNavigator _worldNavigator;
    private readonly StoryProgressTracker _storyTracker;

    private GameRuntime(NarrativeDirector director, WorldNavigator worldNavigator, StoryProgressTracker storyTracker)
    {
        _director = director;
        _worldNavigator = worldNavigator;
        _storyTracker = storyTracker;
    }

    public static GameRuntime CreateDefault()
    {
        var lore = new LoreRepository();
        var orchestrator = new AiOrchestrator();
        var director = new NarrativeDirector(orchestrator, lore);
        var worldNavigator = new WorldNavigator(lore.LoadWorldDefinition());
        var storyTracker = new StoryProgressTracker(lore.LoadStoryDefinition());
        return new GameRuntime(director, worldNavigator, storyTracker);
    }

    public string RenderIntro()
    {
        return _director.BuildIntroductoryScene();
    }

    public WorldSession CreateWorldSession()
    {
        return _worldNavigator.CreateSession();
    }

    public StoryProgressTracker GetStoryTracker()
    {
        return _storyTracker;
    }
}
