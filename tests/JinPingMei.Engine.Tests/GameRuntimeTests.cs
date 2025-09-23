using JinPingMei.Engine;

namespace JinPingMei.Engine.Tests;

public class GameRuntimeTests
{
    [Fact]
    public void RenderIntro_ReturnsContent()
    {
        var runtime = GameRuntime.CreateDefault();

        var intro = runtime.RenderIntro();

        Assert.False(string.IsNullOrWhiteSpace(intro));
    }
}
