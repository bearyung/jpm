using System;
using System.Collections.Generic;
using System.IO;

namespace JinPingMei.ContentPipeline.IO;

internal enum PipelineMode
{
    Extract,
    Template
}

internal sealed record PipelineOptions(
    PipelineMode Mode,
    string InputPath,
    string OutputDirectory,
    string? ChapterId,
    string? TemplateOutputPath)
{
    public static PipelineOptions Parse(string[] args)
    {
        var arguments = args ?? Array.Empty<string>();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < arguments.Length; i++)
        {
            var token = arguments[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = token[2..];
            var value = i + 1 < arguments.Length && !arguments[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? arguments[++i]
                : "true";

            options[key] = value;
        }

        var mode = PipelineMode.Extract;
        if (options.TryGetValue("mode", out var modeValue) && string.Equals(modeValue, "template", StringComparison.OrdinalIgnoreCase))
        {
            mode = PipelineMode.Template;
        }

        var baseDirectory = Directory.GetCurrentDirectory();
        var inputPath = options.TryGetValue("input", out var input)
            ? Path.GetFullPath(input)
            : Path.GetFullPath(Path.Combine(baseDirectory, "data/source-texts/full_version_story.txt"));

        var outputDirectory = options.TryGetValue("output-dir", out var output)
            ? Path.GetFullPath(output)
            : Path.GetFullPath(Path.Combine(baseDirectory, "src/JinPingMei.Content/Data/chapters"));

        string? chapterId = null;
        string? templateOutput = null;

        if (mode == PipelineMode.Template)
        {
            if (!options.TryGetValue("chapter", out chapterId))
            {
                throw new ArgumentException("Template mode requires --chapter <chapter-id>.");
            }

            if (options.TryGetValue("output", out var templatePath))
            {
                templateOutput = Path.GetFullPath(templatePath);
            }
        }

        return new PipelineOptions(mode, inputPath, outputDirectory, chapterId, templateOutput);
    }
}
