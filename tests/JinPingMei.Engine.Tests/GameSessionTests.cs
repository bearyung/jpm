using JinPingMei.Game.Hosting;

namespace JinPingMei.Engine.Tests;

public class GameSessionTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void HandleCommand_EmptyOrWhitespace_ReturnsPlaceholder(string? input)
    {
        var session = new GameSession(GameRuntime.CreateDefault());

        var response = session.HandleCommand(input ?? string.Empty);

        Assert.Contains("指令尚未實作", response);
    }

    [Fact]
    public void HandleCommand_Help_ReturnsHelpText()
    {
        var session = new GameSession(GameRuntime.CreateDefault());

        var response = session.HandleCommand("help");

        Assert.Contains("Prototype", response);
    }
}
