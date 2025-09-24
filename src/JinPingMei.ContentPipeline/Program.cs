using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using JinPingMei.ContentPipeline;
using JinPingMei.ContentPipeline.IO;
using JinPingMei.ContentPipeline.Parsing;

try
{
    var options = PipelineOptions.Parse(args);

    if (!File.Exists(options.InputPath))
    {
        Console.Error.WriteLine($"Input file not found: {options.InputPath}");
        return 1;
    }

    var sourceText = File.ReadAllText(options.InputPath);
    switch (options.Mode)
    {
        case PipelineMode.Extract:
        {
            var service = new ChapterExtractionService();
            var result = service.Extract(sourceText, options.OutputDirectory);

            Console.WriteLine($"Extracted {result.ChapterCount} chapters to: {options.OutputDirectory}");
            Console.WriteLine($"Index file: {result.IndexPath}");
            if (result.FrontMatterPath is not null)
            {
                Console.WriteLine($"Front matter: {result.FrontMatterPath}");
            }

            return 0;
        }
        case PipelineMode.Template:
        {
            var parser = new ChapterParser();
            var parseResult = parser.Parse(sourceText);

            var chapter = parseResult.Chapters.FirstOrDefault(c => string.Equals(c.Id, options.ChapterId, StringComparison.OrdinalIgnoreCase));
            if (chapter is null && int.TryParse(options.ChapterId, out var number))
            {
                chapter = parseResult.Chapters.FirstOrDefault(c => c.Number == number);
            }

            if (chapter is null)
            {
                Console.Error.WriteLine($"Unable to locate chapter '{options.ChapterId}'.");
                return 1;
            }

            var builder = new ChapterTemplateBuilder();
            var request = builder.BuildAnalysisRequest(chapter);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            if (!string.IsNullOrWhiteSpace(options.TemplateOutputPath))
            {
                var directory = Path.GetDirectoryName(options.TemplateOutputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(options.TemplateOutputPath, json);
                Console.WriteLine($"Template written to: {options.TemplateOutputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        default:
            Console.Error.WriteLine($"Unsupported pipeline mode: {options.Mode}");
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Content pipeline failed: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 2;
}
