using System;
using System.IO;
using System.Text.Json;
using JinPingMei.ContentPipeline;
using JinPingMei.ContentPipeline.IO;
using Xunit;

namespace JinPingMei.ContentPipeline.Tests;

public sealed class ChapterExtractionServiceTests
{
    [Fact]
    public void Extract_WritesChapterArtifacts()
    {
        var source = "第一囬　試刀場\n　　段落一。\n第二囬　試火場\n　　段落二。";
        var service = new ChapterExtractionService();
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var result = service.Extract(source, outputDir);

            Assert.True(File.Exists(Path.Combine(outputDir, "chapter-001.json")));
            Assert.True(File.Exists(result.IndexPath));

            var chapterPayload = File.ReadAllText(Path.Combine(outputDir, "chapter-001.json"));
            var artifact = JsonSerializer.Deserialize<ChapterArtifact>(chapterPayload);

            Assert.NotNull(artifact);
            Assert.Equal("chapter-001", artifact!.Id);
            Assert.Contains("段落一", artifact.Text);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }
}
