using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JinPingMei.Engine.Story;

public sealed class VolumeDefinition
{
    [JsonPropertyName("Id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("Title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("EpisodeLabel")]
    public string EpisodeLabel { get; init; } = string.Empty;

    [JsonPropertyName("HubSceneId")]
    public string? HubSceneId { get; init; }

    [JsonPropertyName("HostRoster")]
    public IReadOnlyList<string> HostRoster { get; init; } = Array.Empty<string>();

    [JsonPropertyName("CompletionRule")]
    public string? CompletionRule { get; init; }

    [JsonPropertyName("ChapterIds")]
    public IReadOnlyList<string> ChapterIds { get; init; } = Array.Empty<string>();

    [JsonPropertyName("Synopsis")]
    public string Synopsis { get; init; } = string.Empty;
}
