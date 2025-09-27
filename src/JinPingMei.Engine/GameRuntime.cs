using System.Linq;
using System.Text;
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

    public IntroSequence GetIntroSequence()
    {
        return _director.BuildIntroductoryScene();
    }

    public string RenderIntro()
    {
        var sequence = GetIntroSequence();
        if (sequence.Steps.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var step in sequence.Steps)
        {
            foreach (var line in step)
            {
                builder.AppendLine(line);
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
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
