using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using JinPingMei.ContentPipeline.Parsing;

namespace JinPingMei.ContentPipeline.IO;

internal sealed class ChapterFileWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ChapterFileWriteResult Write(ChapterParseResult parseResult, string outputDirectory)
    {
        if (parseResult is null)
        {
            throw new ArgumentNullException(nameof(parseResult));
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        var indexEntries = new List<ChapterIndexEntry>(parseResult.Chapters.Count);

        foreach (var chapter in parseResult.Chapters)
        {
            var artifact = new ChapterArtifact(
                chapter.Id,
                chapter.Number,
                chapter.Titles,
                chapter.Body);

            var fileName = $"{chapter.Id}.json";
            var filePath = Path.Combine(outputDirectory, fileName);
            var json = JsonSerializer.Serialize(artifact, SerializerOptions);
            File.WriteAllText(filePath, json);

            indexEntries.Add(new ChapterIndexEntry(chapter.Id, chapter.Number, chapter.Titles, fileName));
        }

        var indexPath = Path.Combine(outputDirectory, "index.json");
        var indexPayload = JsonSerializer.Serialize(indexEntries, SerializerOptions);
        File.WriteAllText(indexPath, indexPayload);

        string? frontMatterPath = null;
        if (!string.IsNullOrWhiteSpace(parseResult.FrontMatter))
        {
            frontMatterPath = Path.Combine(outputDirectory, "front-matter.txt");
            File.WriteAllText(frontMatterPath, parseResult.FrontMatter.Trim());
        }

        return new ChapterFileWriteResult(indexPath, frontMatterPath);
    }
}

internal sealed record ChapterArtifact(string Id, int Number, IReadOnlyList<string> Titles, string Text);

internal sealed record ChapterIndexEntry(string Id, int Number, IReadOnlyList<string> Titles, string FileName);

internal sealed record ChapterFileWriteResult(string IndexPath, string? FrontMatterPath);
