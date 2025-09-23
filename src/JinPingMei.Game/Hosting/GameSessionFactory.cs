using System;
using JinPingMei.Engine;
using JinPingMei.Game.Localization;

namespace JinPingMei.Game.Hosting;

public sealed class GameSessionFactory : IGameSessionFactory
{
    private readonly ILocalizationProvider _localization;
    private readonly ITelnetServerDiagnostics _diagnostics;

    public GameSessionFactory(ILocalizationProvider localization, ITelnetServerDiagnostics diagnostics)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public GameSession Create()
    {
        var runtime = GameRuntime.CreateDefault();
        return new GameSession(runtime, _localization, _diagnostics);
    }
}
