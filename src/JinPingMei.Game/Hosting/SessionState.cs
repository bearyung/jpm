namespace JinPingMei.Game.Hosting;

public sealed class SessionState
{
    public string Locale { get; set; } = "zh-TW";

    public string? PlayerName { get; set; }

    public bool HasPlayerName => !string.IsNullOrWhiteSpace(PlayerName);

    public string? CurrentLocaleId { get; set; }

    public string? CurrentSceneId { get; set; }
}
