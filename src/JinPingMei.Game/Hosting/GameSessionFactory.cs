using JinPingMei.Engine;
using JinPingMei.Game.Localization;

namespace JinPingMei.Game.Hosting;

public sealed class GameSessionFactory : IGameSessionFactory
{
    private readonly ILocalizationProvider _localization;

    public GameSessionFactory(ILocalizationProvider localization)
    {
        _localization = localization;
    }

    public GameSession Create()
    {
        var runtime = GameRuntime.CreateDefault();
        return new GameSession(runtime, _localization);
    }
}
