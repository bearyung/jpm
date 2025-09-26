using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using JinPingMei.Content.Story;
using JinPingMei.Content.World;

namespace JinPingMei.Content;

public sealed class LoreRepository
{
    private const string DefaultWorldFileName = "world.zh-TW.json";
    private const string DefaultStoryFileName = "story.zh-TW.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly Lazy<WorldDefinition> _world;
    private readonly Lazy<StoryDefinition> _story;

    public LoreRepository()
    {
        _world = new Lazy<WorldDefinition>(LoadWorldCore);
        _story = new Lazy<StoryDefinition>(LoadStoryCore);
    }

    public string GetOpeningLocale()
    {
        var world = _world.Value;
        var locale = world.Locales.FirstOrDefault(l => string.Equals(l.Id, world.DefaultLocaleId, StringComparison.OrdinalIgnoreCase))
                     ?? world.Locales.FirstOrDefault();

        return locale?.Name ?? "未知地點";
    }

    public WorldDefinition LoadWorldDefinition() => _world.Value;

    public StoryDefinition LoadStoryDefinition() => _story.Value;

    private static WorldDefinition LoadWorldCore()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "Data", DefaultWorldFileName),
            Path.Combine(baseDirectory, DefaultWorldFileName)
        };

        var worldPath = candidates.FirstOrDefault(File.Exists);
        if (worldPath is not null)
        {
            var jsonFromFile = File.ReadAllText(worldPath);
            return ParseWorld(jsonFromFile);
        }

        var assembly = typeof(LoreRepository).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(DefaultWorldFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new FileNotFoundException($"Unable to locate world data file '{DefaultWorldFileName}'. Looked in: {string.Join(", ", candidates)} and embedded resources.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
                          ?? throw new InvalidOperationException($"Embedded world data resource '{resourceName}' could not be opened.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return ParseWorld(json);
    }

    private static WorldDefinition ParseWorld(string json)
    {
        var definition = JsonSerializer.Deserialize<WorldDefinition>(json, JsonOptions) ?? throw new InvalidOperationException("World definition payload was empty.");

        Validate(definition);
        return definition;
    }

    private static void Validate(WorldDefinition definition)
    {
        if (definition.Locales.Count == 0)
        {
            throw new InvalidOperationException("World definition requires at least one locale.");
        }

        if (string.IsNullOrWhiteSpace(definition.DefaultLocaleId))
        {
            throw new InvalidOperationException("World definition requires a default locale id.");
        }

        var defaultLocale = definition.Locales.FirstOrDefault(l => string.Equals(l.Id, definition.DefaultLocaleId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException($"Default locale '{definition.DefaultLocaleId}' could not be found.");

        if (defaultLocale.Scenes.Count == 0)
        {
            throw new InvalidOperationException($"Locale '{defaultLocale.Id}' must define at least one scene.");
        }

        if (string.IsNullOrWhiteSpace(defaultLocale.DefaultSceneId))
        {
            throw new InvalidOperationException($"Locale '{defaultLocale.Id}' must declare a default scene id.");
        }

        _ = defaultLocale.Scenes.FirstOrDefault(s => string.Equals(s.Id, defaultLocale.DefaultSceneId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Default scene '{defaultLocale.DefaultSceneId}' could not be found within locale '{defaultLocale.Id}'.");
    }

    private static StoryDefinition LoadStoryCore()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "Data", DefaultStoryFileName),
            Path.Combine(baseDirectory, DefaultStoryFileName)
        };

        var storyPath = candidates.FirstOrDefault(File.Exists);
        if (storyPath is not null)
        {
            var jsonFromFile = File.ReadAllText(storyPath);
            return ParseStory(jsonFromFile);
        }

        var assembly = typeof(LoreRepository).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(DefaultStoryFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return CreateDefaultStory();
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
                          ?? throw new InvalidOperationException($"Embedded story data resource '{resourceName}' could not be opened.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return ParseStory(json);
    }

    private static StoryDefinition ParseStory(string json)
    {
        var definition = JsonSerializer.Deserialize<StoryDefinition>(json, JsonOptions) ?? throw new InvalidOperationException("Story definition payload was empty.");

        ValidateStory(definition);
        return definition;
    }

    private static void ValidateStory(StoryDefinition definition)
    {
        if (definition.Volumes.Count == 0)
        {
            throw new InvalidOperationException("Story definition requires at least one volume.");
        }

        foreach (var volume in definition.Volumes)
        {
            if (volume.Episodes.Count == 0)
            {
                throw new InvalidOperationException($"Volume '{volume.Id}' must have at least one episode.");
            }

            foreach (var episode in volume.Episodes)
            {
                if (episode.AvailableCharacters.Count == 0)
                {
                    throw new InvalidOperationException($"Episode '{episode.Id}' must have at least one available character.");
                }
            }
        }
    }

    private static StoryDefinition CreateDefaultStory()
    {
        return new StoryDefinition
        {
            Id = "jinpingmei",
            Title = "金瓶梅",
            Description = "A story of desire and consequence in Ming dynasty China",
            Volumes = new[]
            {
                new VolumeDefinition
                {
                    Id = "volume1",
                    VolumeNumber = 1,
                    Title = "初遇",
                    Description = "The beginning of intertwined fates",
                    Episodes = new[]
                    {
                        new EpisodeDefinition
                        {
                            Id = "episode1",
                            EpisodeNumber = 1,
                            Title = "清河縣",
                            Description = "Life begins in Qinghe County",
                            StartingSceneId = "qinghe_market",
                            StartingLocaleId = "qinghe",
                            CompletionSceneIds = new[] { "qinghe_market", "qinghe_teahouse" },
                            AvailableCharacters = new[]
                            {
                                new CharacterDefinition
                                {
                                    Id = "ximen_qing",
                                    Name = "西門慶",
                                    Description = "A wealthy merchant with boundless ambition",
                                    BackgroundStory = "Born into a merchant family, you have built your fortune through cunning and charm.",
                                    Traits = new CharacterTraits
                                    {
                                        Personality = "Charismatic, ambitious, hedonistic",
                                        SocialStatus = "Wealthy merchant",
                                        Occupation = "Merchant and landowner",
                                        Skills = new[] { "Business", "Negotiation", "Seduction" },
                                        Relationships = new[] { "Friend of Ying Bojue", "Acquaintance of Hua Zixu" }
                                    }
                                },
                                new CharacterDefinition
                                {
                                    Id = "pan_jinlian",
                                    Name = "潘金蓮",
                                    Description = "A beautiful woman trapped in an unhappy marriage",
                                    BackgroundStory = "Married to the dwarf Wu Da, you long for passion and recognition.",
                                    Traits = new CharacterTraits
                                    {
                                        Personality = "Beautiful, cunning, passionate",
                                        SocialStatus = "Merchant's wife",
                                        Occupation = "Housewife",
                                        Skills = new[] { "Household management", "Embroidery", "Manipulation" },
                                        Relationships = new[] { "Wife of Wu Da", "Sister-in-law of Wu Song" }
                                    }
                                },
                                new CharacterDefinition
                                {
                                    Id = "li_pinger",
                                    Name = "李瓶兒",
                                    Description = "A widow with hidden wealth and secrets",
                                    BackgroundStory = "Recently widowed, you possess both beauty and a mysterious fortune.",
                                    Traits = new CharacterTraits
                                    {
                                        Personality = "Gentle, secretive, romantic",
                                        SocialStatus = "Wealthy widow",
                                        Occupation = "Property owner",
                                        Skills = new[] { "Finance", "Secret-keeping", "Social grace" },
                                        Relationships = new[] { "Former wife of Hua Zixu" }
                                    }
                                }
                            },
                            AllowRandomCharacterSelection = true
                        }
                    }
                }
            }
        };
    }
}
