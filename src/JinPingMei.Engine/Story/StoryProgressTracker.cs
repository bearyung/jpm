using System;
using System.Collections.Generic;
using System.Linq;
using JinPingMei.Content.Story;

namespace JinPingMei.Engine.Story;

public sealed class StoryProgressTracker
{
    private readonly StoryDefinition _story;
    private readonly Dictionary<string, EpisodeProgress> _episodeProgress;
    private VolumeDefinition? _currentVolume;
    private EpisodeDefinition? _currentEpisode;
    private CharacterDefinition? _currentCharacter;

    public StoryProgressTracker(StoryDefinition story)
    {
        _story = story ?? throw new ArgumentNullException(nameof(story));
        _episodeProgress = new Dictionary<string, EpisodeProgress>();
    }

    public StoryDefinition Story => _story;

    public VolumeDefinition? CurrentVolume => _currentVolume;

    public EpisodeDefinition? CurrentEpisode => _currentEpisode;

    public CharacterDefinition? CurrentCharacter => _currentCharacter;

    public bool IsCharacterLocked => _currentEpisode != null && _currentCharacter != null;

    public bool IsEpisodeComplete(string episodeId)
    {
        return _episodeProgress.TryGetValue(episodeId, out var progress) && progress.IsCompleted;
    }

    public bool TryStartNewEpisode(string volumeId, string episodeId, out EpisodeDefinition episode)
    {
        episode = null!;

        var volume = _story.Volumes.FirstOrDefault(v => v.Id == volumeId);
        if (volume == null) return false;

        episode = volume.Episodes.FirstOrDefault(e => e.Id == episodeId);
        if (episode == null) return false;

        if (_currentEpisode != null && !IsEpisodeComplete(_currentEpisode.Id))
        {
            return false;
        }

        _currentVolume = volume;
        _currentEpisode = episode;
        _currentCharacter = null;

        if (!_episodeProgress.ContainsKey(episodeId))
        {
            _episodeProgress[episodeId] = new EpisodeProgress(episodeId);
        }

        return true;
    }

    public bool TrySelectCharacter(string characterId)
    {
        if (_currentEpisode == null || _currentCharacter != null)
        {
            return false;
        }

        var character = _currentEpisode.AvailableCharacters.FirstOrDefault(c => c.Id == characterId);
        if (character == null) return false;

        _currentCharacter = character;

        if (_episodeProgress.TryGetValue(_currentEpisode.Id, out var progress))
        {
            progress.SelectedCharacterId = characterId;
            progress.StartTime ??= DateTime.UtcNow;
        }

        return true;
    }

    public CharacterDefinition? SelectRandomCharacter()
    {
        if (_currentEpisode == null ||
            _currentCharacter != null ||
            !_currentEpisode.AllowRandomCharacterSelection ||
            !_currentEpisode.AvailableCharacters.Any())
        {
            return null;
        }

        var random = new Random();
        var index = random.Next(_currentEpisode.AvailableCharacters.Count);
        var character = _currentEpisode.AvailableCharacters[index];

        if (TrySelectCharacter(character.Id))
        {
            return character;
        }

        return null;
    }

    public bool TryCompleteCurrentEpisode(string completionSceneId)
    {
        if (_currentEpisode == null || _currentCharacter == null)
        {
            return false;
        }

        if (!_currentEpisode.CompletionSceneIds.Contains(completionSceneId))
        {
            return false;
        }

        if (_episodeProgress.TryGetValue(_currentEpisode.Id, out var progress))
        {
            progress.IsCompleted = true;
            progress.CompletionTime = DateTime.UtcNow;
            progress.CompletionSceneId = completionSceneId;
        }

        return true;
    }

    public EpisodeDefinition? GetNextEpisode()
    {
        if (_currentVolume == null || _currentEpisode == null)
        {
            return _story.Volumes.FirstOrDefault()?.Episodes.FirstOrDefault();
        }

        var currentEpisodeIndex = _currentVolume.Episodes
            .Select((e, i) => new { Episode = e, Index = i })
            .FirstOrDefault(x => x.Episode.Id == _currentEpisode.Id)?.Index ?? -1;

        if (currentEpisodeIndex >= 0 && currentEpisodeIndex < _currentVolume.Episodes.Count - 1)
        {
            return _currentVolume.Episodes[currentEpisodeIndex + 1];
        }

        var currentVolumeIndex = _story.Volumes
            .Select((v, i) => new { Volume = v, Index = i })
            .FirstOrDefault(x => x.Volume.Id == _currentVolume.Id)?.Index ?? -1;

        if (currentVolumeIndex >= 0 && currentVolumeIndex < _story.Volumes.Count - 1)
        {
            var nextVolume = _story.Volumes[currentVolumeIndex + 1];
            return nextVolume.Episodes.FirstOrDefault();
        }

        return null;
    }

    private sealed class EpisodeProgress
    {
        public string EpisodeId { get; }
        public string? SelectedCharacterId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public bool IsCompleted { get; set; }
        public string? CompletionSceneId { get; set; }

        public EpisodeProgress(string episodeId)
        {
            EpisodeId = episodeId;
        }
    }
}