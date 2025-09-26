using System;
using System.IO;
using JinPingMei.ContentPipeline.IO;
using JinPingMei.ContentPipeline.Parsing;

namespace JinPingMei.ContentPipeline;

internal sealed class ChapterExtractionService
{
    private readonly ChapterParser _parser = new();
    private readonly ChapterFileWriter _writer = new();

    public ChapterExtractionResult Extract(string sourceText, string outputDirectory)
    {
        if (sourceText is null)
        {
            throw new ArgumentNullException(nameof(sourceText));
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        var parseResult = _parser.Parse(sourceText);
        Directory.CreateDirectory(outputDirectory);

        var result = _writer.Write(parseResult, outputDirectory);
        return new ChapterExtractionResult(parseResult.Chapters.Count, result.IndexPath, result.FrontMatterPath);
    }

    public ChapterParseResult Parse(string sourceText)
    {
        if (sourceText is null)
        {
            throw new ArgumentNullException(nameof(sourceText));
        }

        return _parser.Parse(sourceText);
    }
}

internal sealed record ChapterExtractionResult(int ChapterCount, string IndexPath, string? FrontMatterPath);
