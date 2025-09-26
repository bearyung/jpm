using JinPingMei.ContentPipeline.Parsing;
using Xunit;

namespace JinPingMei.ContentPipeline.Tests;

public sealed class ChineseNumberParserTests
{
    [Theory]
    [InlineData("一", 1)]
    [InlineData("十", 10)]
    [InlineData("十一", 11)]
    [InlineData("二十", 20)]
    [InlineData("三十五", 35)]
    [InlineData("一百", 100)]
    [InlineData("一百零二", 102)]
    [InlineData("一百一十", 110)]
    [InlineData("一百二十三", 123)]
    public void Parse_ReturnsExpectedValue(string token, int expected)
    {
        var number = ChineseNumberParser.Parse(token);
        Assert.Equal(expected, number);
    }
}
