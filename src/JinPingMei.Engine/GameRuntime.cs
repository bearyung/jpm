using System.Linq;
using JinPingMei.AI;
using JinPingMei.Content;
using JinPingMei.Engine.World;

namespace JinPingMei.Engine;

public sealed class GameRuntime
{
    private readonly NarrativeDirector _director;
    private readonly WorldNavigator _worldNavigator;
    private readonly Story.StoryRepository _storyRepository;

    private GameRuntime(NarrativeDirector director, WorldNavigator worldNavigator, Story.StoryRepository storyRepository)
    {
        _director = director;
        _worldNavigator = worldNavigator;
        _storyRepository = storyRepository;
    }

    public static GameRuntime CreateDefault()
    {
        var lore = new LoreRepository();
        var orchestrator = new AiOrchestrator();
        var director = new NarrativeDirector(orchestrator, lore);
        var worldNavigator = new WorldNavigator(lore.LoadWorldDefinition());
        var storyRepository = new Story.StoryRepository();
        return new GameRuntime(director, worldNavigator, storyRepository);
    }

    public string RenderIntro()
    {
        return _director.BuildIntroductoryScene();
    }

    public WorldSession CreateWorldSession()
    {
        return _worldNavigator.CreateSession();
    }

    public Story.StorySession CreateStorySession(string volumeId)
    {
        var volume = _storyRepository.GetVolume(volumeId);
        var chapters = volume.ChapterIds
            .Select(id => _storyRepository.LoadChapter(id))
            .ToList();

        return new Story.StorySession(volume, chapters);
    }
}
