using System;
using System.Linq;
using JinPingMei.Game.Hosting;
using JinPingMei.Game.Localization;
using JinPingMei.Engine;

namespace JinPingMei.Engine.Tests;

public class GameSessionTests
{
    private readonly ILocalizationProvider _localization = new TestLocalizationProvider();
    private readonly TestDiagnostics _diagnostics = new();

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
    public void Constructor_SetsInitialWorldState()
    {
        var session = CreateSession();

        Assert.Equal("qinghe", session.State.CurrentLocaleId);
        Assert.Equal("market", session.State.CurrentSceneId);
    }

    [Fact]
    public void HandleInput_LookCommand_ReturnsSceneOverview()
    {
        var session = CreateSession();

        var result = session.HandleInput("/look");

        Assert.Contains(result.Lines, line => line.Contains("清河集市"));
        Assert.Contains(result.Lines, line => line.Contains("可前往"));
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

    [Fact]
    public void HandleInput_GoCommand_MovesPlayerToTargetScene()
    {
        var session = CreateSession();

        var result = session.HandleInput("/go 茶樓");

        Assert.Equal("teahouse", session.State.CurrentSceneId);
        Assert.Contains(result.Lines, line => line.Contains("會仙樓茶館"));
    }

    [Fact]
    public void HandleInput_GoCommand_InvalidDestination()
    {
        var session = CreateSession();

        var result = session.HandleInput("/go 月宮");

        Assert.Contains("無法辨識", result.Lines.Single());
    }

    [Fact]
    public void HandleInput_ExamineScene_ReturnsDetailedDescription()
    {
        var session = CreateSession();

        var result = session.HandleInput("/examine scene");

        Assert.Contains(result.Lines, line => line.Contains("可通往"));
        Assert.Contains(result.Lines, line => line.Contains("叫賣的商販"));
    }

    [Fact]
    public void HandleInput_ExamineNpc_ReturnsNpcInsight()
    {
        var session = CreateSession();

        var result = session.HandleInput("/examine 商販");

        Assert.Contains(result.Lines, line => line.Contains("你注視著"));
        Assert.Contains(result.Lines, line => line.Contains("叫賣的商販"));
    }

    [Fact]
    public void HandleInput_DiagnosticsCommand_ReturnsSnapshot()
    {
        var session = CreateSession();
        _diagnostics.Snapshot = new TelnetServerSnapshot(
            DateTimeOffset.UtcNow.AddMinutes(-10),
            TimeSpan.FromMinutes(10),
            ActiveSessions: 3,
            TotalSessions: 42,
            CompletedSessions: 39,
            RejectedSessions: 2,
            SessionErrors: 1,
            CommandErrors: 2,
            InactivityTimeouts: 1,
            LifetimeEnforcements: 0,
            TotalCommands: 150);

        var result = session.HandleInput("/diagnostics");

        Assert.Contains(result.Lines, line => line.Contains("系統診斷資訊"));
        Assert.Contains(result.Lines, line => line.Contains("42"));
        Assert.Contains(result.Lines, line => line.Contains("指令執行次數"));
    }

    [Fact]
    public void HandleInput_HealthCommand_ReflectsStatus()
    {
        var session = CreateSession();
        _diagnostics.Snapshot = new TelnetServerSnapshot(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            TimeSpan.FromMinutes(1),
            ActiveSessions: 1,
            TotalSessions: 2,
            CompletedSessions: 1,
            RejectedSessions: 0,
            SessionErrors: 0,
            CommandErrors: 0,
            InactivityTimeouts: 0,
            LifetimeEnforcements: 0,
            TotalCommands: 10);

        var result = session.HandleInput("/health");

        Assert.Contains(result.Lines, line => line.Contains("系統狀態：正常"));
        Assert.Contains(result.Lines, line => line.Contains("當前連線數"));
    }

    private GameSession CreateSession()
    {
        var runtime = GameRuntime.CreateDefault();
        return new GameSession(runtime, _localization, _diagnostics);
    }

    private sealed class TestDiagnostics : ITelnetServerDiagnostics
    {
        public TelnetServerSnapshot Snapshot { get; set; } = new TelnetServerSnapshot(
            DateTimeOffset.UtcNow,
            TimeSpan.Zero,
            ActiveSessions: 0,
            TotalSessions: 0,
            CompletedSessions: 0,
            RejectedSessions: 0,
            SessionErrors: 0,
            CommandErrors: 0,
            InactivityTimeouts: 0,
            LifetimeEnforcements: 0,
            TotalCommands: 0);

        public TelnetServerSnapshot CaptureSnapshot() => Snapshot;
    }
}
