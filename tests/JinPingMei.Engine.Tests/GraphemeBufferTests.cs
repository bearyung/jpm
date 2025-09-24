using System.Text;
using JinPingMei.Game.Hosting.Text;
using Xunit;

namespace JinPingMei.Engine.Tests;

public class GraphemeBufferTests
{
    [Fact]
    public void Backspace_CjkCharacter_UsesDoubleWidth()
    {
        var buffer = new GraphemeBuffer();
        buffer.Append(new Rune('中'));

        var removed = buffer.TryBackspace(out var width);

        Assert.True(removed);
        Assert.Equal(2, width);
    }

    [Fact]
    public void Backspace_EmojiSequence_RemovesAsSingleCluster()
    {
        var buffer = new GraphemeBuffer();
        foreach (var rune in "👍🏽".EnumerateRunes())
        {
            buffer.Append(rune);
        }

        var removed = buffer.TryBackspace(out var width);

        Assert.True(removed);
        Assert.Equal(2, width);
    }

    [Fact]
    public void Backspace_CombiningMarkSequence_TreatedAsWidthOne()
    {
        var buffer = new GraphemeBuffer();
        foreach (var rune in "a\u0301".EnumerateRunes())
        {
            buffer.Append(rune);
        }

        var removed = buffer.TryBackspace(out var width);

        Assert.True(removed);
        Assert.Equal(1, width);
    }

    [Fact]
    public void Drain_ReturnsFullTextAndClears()
    {
        var buffer = new GraphemeBuffer();
        foreach (var rune in "中文".EnumerateRunes())
        {
            buffer.Append(rune);
        }

        var drained = buffer.TryDrain(out var text);

        Assert.True(drained);
        Assert.Equal("中文", text);
        Assert.False(buffer.TryDrain(out var empty));
        Assert.Equal(string.Empty, empty);
    }

    [Fact]
    public void Backspace_MultipleCjkCharacters_RemovesOneAtATime()
    {
        var buffer = new GraphemeBuffer();
        foreach (var rune in "中文".EnumerateRunes())
        {
            buffer.Append(rune);
        }

        var removed1 = buffer.TryBackspace(out var width1);
        Assert.True(removed1);
        Assert.Equal(2, width1);

        var drained = buffer.TryDrain(out var remaining);
        Assert.True(drained);
        Assert.Equal("中", remaining);
    }

    [Theory]
    [InlineData('中', 2)]
    [InlineData('文', 2)]
    [InlineData('あ', 2)]
    [InlineData('ア', 2)]
    [InlineData('가', 2)]
    [InlineData('a', 1)]
    [InlineData(' ', 1)]
    public void GetDisplayWidth_VariousCharacters_ReturnsCorrectWidth(char ch, int expectedWidth)
    {
        var buffer = new GraphemeBuffer();
        buffer.Append(new Rune(ch));

        var removed = buffer.TryBackspace(out var width);

        Assert.True(removed);
        Assert.Equal(expectedWidth, width);
    }

    [Fact]
    public void Backspace_EmojiWithSkinTone_RemovesCompleteSequence()
    {
        var buffer = new GraphemeBuffer();
        foreach (var rune in "👍🏽".EnumerateRunes())
        {
            buffer.Append(rune);
        }

        var removed = buffer.TryBackspace(out var width);
        Assert.True(removed);
        Assert.Equal(2, width);

        var drained = buffer.TryDrain(out var text);
        Assert.False(drained);
        Assert.Equal(string.Empty, text);
    }
}
