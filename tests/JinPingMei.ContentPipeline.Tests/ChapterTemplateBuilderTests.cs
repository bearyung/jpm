using JinPingMei.ContentPipeline;
using JinPingMei.ContentPipeline.Parsing;
using Xunit;

namespace JinPingMei.ContentPipeline.Tests;

public sealed class ChapterTemplateBuilderTests
{
    [Fact]
    public void BuildAnalysisRequest_ProducesInstruction()
    {
        var parser = new ChapterParser();
        var source = "第一囬　景陽岡武松打虎\n　　話說武松大醉上景陽岡。";
        var parseResult = parser.Parse(source);
        var chapter = parseResult.Chapters[0];

        var builder = new ChapterTemplateBuilder();
        var request = builder.BuildAnalysisRequest(chapter);

        Assert.Equal("chapter-001", request.ChapterId);
        Assert.Contains("chapterId", request.Instruction);
        Assert.Contains("武松", request.ChapterText);
    }
}
