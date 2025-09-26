using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using JinPingMei.Content.World;

namespace JinPingMei.Content;

public sealed class LoreRepository
{
    private const string DefaultWorldFileName = "world.zh-TW.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly Lazy<WorldDefinition> _world;

    public LoreRepository()
    {
        _world = new Lazy<WorldDefinition>(LoadWorldCore);
    }

    public string GetOpeningLocale()
    {
        var world = _world.Value;
        var locale = world.Locales.FirstOrDefault(l => string.Equals(l.Id, world.DefaultLocaleId, StringComparison.OrdinalIgnoreCase))
                     ?? world.Locales.FirstOrDefault();

        return locale?.Name ?? "未知地點";
    }

    public WorldDefinition LoadWorldDefinition() => _world.Value;

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
}