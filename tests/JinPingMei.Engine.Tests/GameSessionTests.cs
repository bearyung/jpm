using System.Linq;
using JinPingMei.Game.Hosting;
using JinPingMei.Game.Localization;
using JinPingMei.Engine;

namespace JinPingMei.Engine.Tests;

public class GameSessionTests
{
    private readonly ILocalizationProvider _localization = new TestLocalizationProvider();

    [Fact]
    public void HandleInput_NameCommand_SetsPlayerName()
    {
        var session = CreateSession();

        var result = session.HandleInput("/name 西門慶");

        Assert.Equal("西門慶", session.State.PlayerName);
        Assert.Contains("西門慶", result.Lines.Single());
    }

    [Fact]
    public void HandleInput_SayWithoutName_UsesDefaultDisplayName()
    {
        var session = CreateSession();

        var result = session.HandleInput("/say 我在此聆聽。");

        Assert.Single(result.Lines);
        Assert.Contains(_localization.GetString("zh-TW", "session.display_name.default"), result.Lines.Single());
    }

    [Fact]
    public void HandleInput_FreeText_ProducesPlaceholderNarration()
    {
        var session = CreateSession();
        session.HandleInput("/name 金蓮");

        var result = session.HandleInput("我望向窗外的月色。");

        Assert.Single(result.Lines);
        Assert.Contains("金蓮", result.Lines.Single());
    }

    [Fact]
    public void HandleInput_QuitCommand_RequestsDisconnect()
    {
        var session = CreateSession();

        var result = session.HandleInput("/quit");

        Assert.True(result.ShouldDisconnect);
    }

    [Fact]
    public void HandleInput_UnknownCommand_ReturnsLocalizedMessage()
    {
        var session = CreateSession();

        var result = session.HandleInput("/dance");

        Assert.Contains("無法識別", result.Lines.Single());
    }

    private GameSession CreateSession()
    {
        var runtime = GameRuntime.CreateDefault();
        return new GameSession(runtime, _localization);
    }
}
