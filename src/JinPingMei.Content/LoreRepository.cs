using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using JinPingMei.Content.Narrative;
using JinPingMei.Content.World;

namespace JinPingMei.Content;

public sealed class LoreRepository
{
    private const string DefaultWorldFileName = "world.zh-TW.json";
    private const string DefaultIntroFileName = "intro.zh-TW.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly Lazy<WorldDefinition> _world;
    private readonly Lazy<IntroDefinition> _intro;

    public LoreRepository()
    {
        _world = new Lazy<WorldDefinition>(LoadWorldCore);
        _intro = new Lazy<IntroDefinition>(LoadIntroCore);
    }

    public string GetOpeningLocale()
    {
        return GetOpeningLocaleDefinition().Name;
    }

    public LocaleDefinition GetOpeningLocaleDefinition()
    {
        var world = _world.Value;
        var locale = world.Locales.FirstOrDefault(l => string.Equals(l.Id, world.DefaultLocaleId, StringComparison.OrdinalIgnoreCase))
                     ?? world.Locales.FirstOrDefault();

        return locale ?? throw new InvalidOperationException("World definition did not contain a default locale.");
    }

    public IReadOnlyList<IReadOnlyList<string>> GetOpeningNarrativeSteps(string? localeId)
    {
        var intro = _intro.Value;

        if (intro.Scripts.Count == 0)
        {
            return Array.Empty<IReadOnlyList<string>>();
        }

        IntroLocaleScript? script = null;

        if (!string.IsNullOrWhiteSpace(localeId))
        {
            script = intro.Scripts.FirstOrDefault(s => string.Equals(s.LocaleId, localeId, StringComparison.OrdinalIgnoreCase));
        }

        script ??= intro.Scripts.FirstOrDefault(s => string.Equals(s.LocaleId, intro.DefaultLocaleId, StringComparison.OrdinalIgnoreCase));
        script ??= intro.Scripts.FirstOrDefault();

        if (script is null)
        {
            return Array.Empty<IReadOnlyList<string>>();
        }

        if (script.Steps.Count == 0)
        {
            return Array.Empty<IReadOnlyList<string>>();
        }

        return script.Steps
            .Select(step => (IReadOnlyList<string>)step.Lines)
            .Where(lines => lines.Count > 0)
            .ToArray();
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

    private static IntroDefinition LoadIntroCore()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "Data", DefaultIntroFileName),
            Path.Combine(baseDirectory, DefaultIntroFileName)
        };

        var introPath = candidates.FirstOrDefault(File.Exists);
        if (introPath is not null)
        {
            var jsonFromFile = File.ReadAllText(introPath);
            return ParseIntro(jsonFromFile);
        }

        var assembly = typeof(LoreRepository).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(DefaultIntroFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new FileNotFoundException($"Unable to locate intro narrative data file '{DefaultIntroFileName}'. Looked in: {string.Join(", ", candidates)} and embedded resources.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
                          ?? throw new InvalidOperationException($"Embedded intro narrative data resource '{resourceName}' could not be opened.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return ParseIntro(json);
    }

    private static WorldDefinition ParseWorld(string json)
    {
        var definition = JsonSerializer.Deserialize<WorldDefinition>(json, JsonOptions) ?? throw new InvalidOperationException("World definition payload was empty.");

        Validate(definition);
        return definition;
    }

    private static IntroDefinition ParseIntro(string json)
    {
        var definition = JsonSerializer.Deserialize<IntroDefinition>(json, JsonOptions) ?? throw new InvalidOperationException("Intro narrative payload was empty.");

        ValidateIntro(definition);
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

    private static void ValidateIntro(IntroDefinition definition)
    {
        if (definition.Scripts.Count == 0)
        {
            throw new InvalidOperationException("Intro narrative requires at least one script.");
        }

        if (string.IsNullOrWhiteSpace(definition.DefaultLocaleId))
        {
            throw new InvalidOperationException("Intro narrative requires a default locale id.");
        }

        foreach (var script in definition.Scripts)
        {
            if (string.IsNullOrWhiteSpace(script.LocaleId))
            {
                throw new InvalidOperationException("Intro narrative scripts must declare a locale id.");
            }

            if (script.Steps.Count == 0)
            {
                throw new InvalidOperationException($"Intro narrative script for locale '{script.LocaleId}' must include at least one step.");
            }

            foreach (var step in script.Steps)
            {
                if (step.Lines.Count == 0)
                {
                    throw new InvalidOperationException($"Intro narrative step for locale '{script.LocaleId}' contains no lines.");
                }
            }
        }
    }
}
