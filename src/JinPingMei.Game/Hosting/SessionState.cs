using JinPingMei.Content.Story;

namespace JinPingMei.Game.Hosting;

public sealed class SessionState
{
    public string Locale { get; set; } = "zh-TW";

    public string? PlayerName { get; set; }

    public bool HasPlayerName => !string.IsNullOrWhiteSpace(PlayerName);

    public string? CurrentLocaleId { get; set; }

    public string? CurrentSceneId { get; set; }

    public string? CurrentVolumeId { get; set; }

    public string? CurrentEpisodeId { get; set; }

    public CharacterDefinition? CurrentCharacter { get; set; }

    public bool IsInCharacterSelection { get; set; }

    public bool IsCharacterLocked => CurrentCharacter != null && CurrentEpisodeId != null;
}
