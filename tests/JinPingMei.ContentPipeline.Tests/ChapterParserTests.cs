using JinPingMei.ContentPipeline.Parsing;
using Xunit;

namespace JinPingMei.ContentPipeline.Tests;

public sealed class ChapterParserTests
{
    [Fact]
    public void Parse_ExtractsFrontMatterAndChapters()
    {
        var source = "序言內容\n\n第一囬　試刀場\n　　段落一。\n　　段落二。\n第二囬　試火場\n　　新章節。";
        var parser = new ChapterParser();

        var result = parser.Parse(source);

        Assert.Equal("序言內容", result.FrontMatter);
        Assert.Equal(2, result.Chapters.Count);

        var first = result.Chapters[0];
        Assert.Equal("chapter-001", first.Id);
        Assert.Equal(1, first.Number);
        Assert.Equal("試刀場", first.Titles[0]);
        Assert.Contains("段落一", first.Body);
    }
}
