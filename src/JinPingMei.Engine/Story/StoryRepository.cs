using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JinPingMei.Engine.Story;

public sealed class StoryRepository
{
    private readonly string _contentRoot;
    private readonly ConcurrentDictionary<string, ChapterDefinition> _chapterCache = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<VolumeDefinition>? _volumes;

    public StoryRepository(string? contentRoot = null)
    {
        var basePath = contentRoot ?? AppContext.BaseDirectory;
        _contentRoot = Path.Combine(basePath, "Data");
    }

    public IReadOnlyList<VolumeDefinition> LoadVolumes()
    {
        if (_volumes is not null)
        {
            return _volumes;
        }

        var path = Path.Combine(_contentRoot, "volumes.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Unable to locate volumes manifest at '{path}'.");
        }

        using var stream = File.OpenRead(path);
        var volumes = JsonSerializer.Deserialize<IReadOnlyList<VolumeDefinition>>(stream, JsonOptions.Default)
                      ?? Array.Empty<VolumeDefinition>();

        _volumes = volumes;
        return _volumes;
    }

    public VolumeDefinition GetVolume(string volumeId)
    {
        var volumes = LoadVolumes();
        return volumes.FirstOrDefault(v => string.Equals(v.Id, volumeId, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Volume '{volumeId}' was not found.");
    }

    public ChapterDefinition LoadChapter(string chapterId)
    {
        if (_chapterCache.TryGetValue(chapterId, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(_contentRoot, "chapters", $"{chapterId}.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Chapter file '{path}' was not found.");
        }

        using var stream = File.OpenRead(path);
        var chapter = JsonSerializer.Deserialize<ChapterDefinition>(stream, JsonOptions.Default)
                      ?? throw new InvalidOperationException($"Chapter '{chapterId}' could not be deserialized.");

        _chapterCache[chapterId] = chapter;
        return chapter;
    }
}

internal static class JsonOptions
{
    private static JsonSerializerOptions? _options;

    public static JsonSerializerOptions Default => _options ??= Create();

    private static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }
}
