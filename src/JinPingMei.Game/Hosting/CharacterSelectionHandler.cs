using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JinPingMei.Content.Story;
using JinPingMei.Game.Localization;

namespace JinPingMei.Game.Hosting;

public sealed class CharacterSelectionHandler
{
    private readonly ILocalizationProvider _localization;
    private readonly string _locale;

    public CharacterSelectionHandler(ILocalizationProvider localization, string locale)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _locale = locale ?? throw new ArgumentNullException(nameof(locale));
    }

    public string RenderCharacterSelectionPrompt(EpisodeDefinition episode, VolumeDefinition volume)
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"  📖 Volume {volume.VolumeNumber}: {volume.Title}");
        sb.AppendLine($"  📜 Episode {episode.EpisodeNumber}: {episode.Title}");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("  🎭 SELECT YOUR CHARACTER FOR THIS EPISODE");
        sb.AppendLine();
        sb.AppendLine("  You will embody this character throughout the entire episode.");
        sb.AppendLine("  Choose wisely - you cannot change until the next episode!");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine();

        for (int i = 0; i < episode.AvailableCharacters.Count; i++)
        {
            var character = episode.AvailableCharacters[i];
            sb.AppendLine($"  [{i + 1}] {character.Name}");
            sb.AppendLine($"      {character.Description}");

            if (!string.IsNullOrWhiteSpace(character.Traits.Occupation))
            {
                sb.AppendLine($"      Occupation: {character.Traits.Occupation}");
            }

            if (!string.IsNullOrWhiteSpace(character.Traits.SocialStatus))
            {
                sb.AppendLine($"      Status: {character.Traits.SocialStatus}");
            }

            sb.AppendLine();
        }

        if (episode.AllowRandomCharacterSelection)
        {
            sb.AppendLine($"  [R] Random Selection");
            sb.AppendLine($"      Let fate decide your role in this story");
            sb.AppendLine();
        }

        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.Append("  Enter your choice: ");

        return sb.ToString();
    }

    public string RenderCharacterSelected(CharacterDefinition character, EpisodeDefinition episode)
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"  ✨ You have become: {character.Name}");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"  {character.Description}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(character.BackgroundStory))
        {
            sb.AppendLine("  Background:");
            sb.AppendLine($"  {character.BackgroundStory}");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(character.Traits.Personality))
        {
            sb.AppendLine($"  Personality: {character.Traits.Personality}");
        }

        if (character.Traits.Skills?.Any() == true)
        {
            sb.AppendLine($"  Skills: {string.Join(", ", character.Traits.Skills)}");
        }

        if (character.Traits.Relationships?.Any() == true)
        {
            sb.AppendLine($"  Relationships: {string.Join(", ", character.Traits.Relationships)}");
        }

        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine($"  Episode {episode.EpisodeNumber}: {episode.Title}");
        sb.AppendLine($"  {episode.Description}");
        sb.AppendLine();
        sb.AppendLine("  Your journey begins...");
        sb.AppendLine();

        return sb.ToString();
    }

    public CharacterSelectionResult ParseCharacterSelection(string input, EpisodeDefinition episode)
    {
        var trimmed = input?.Trim() ?? string.Empty;

        if (trimmed.Equals("R", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("RANDOM", StringComparison.OrdinalIgnoreCase))
        {
            if (episode.AllowRandomCharacterSelection)
            {
                return CharacterSelectionResult.RandomSelection();
            }

            return CharacterSelectionResult.Invalid("Random selection is not available for this episode.");
        }

        if (int.TryParse(trimmed, out var selection))
        {
            if (selection >= 1 && selection <= episode.AvailableCharacters.Count)
            {
                var character = episode.AvailableCharacters[selection - 1];
                return CharacterSelectionResult.Selected(character);
            }

            return CharacterSelectionResult.Invalid($"Please select a number between 1 and {episode.AvailableCharacters.Count}.");
        }

        var matchedCharacter = episode.AvailableCharacters
            .FirstOrDefault(c => c.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

        if (matchedCharacter != null)
        {
            return CharacterSelectionResult.Selected(matchedCharacter);
        }

        return CharacterSelectionResult.Invalid("Invalid selection. Please enter a number or 'R' for random.");
    }
}

public sealed class CharacterSelectionResult
{
    public bool IsValid { get; private init; }
    public bool IsRandom { get; private init; }
    public CharacterDefinition? SelectedCharacter { get; private init; }
    public string? ErrorMessage { get; private init; }

    private CharacterSelectionResult() { }

    public static CharacterSelectionResult Selected(CharacterDefinition character)
    {
        return new CharacterSelectionResult
        {
            IsValid = true,
            IsRandom = false,
            SelectedCharacter = character
        };
    }

    public static CharacterSelectionResult RandomSelection()
    {
        return new CharacterSelectionResult
        {
            IsValid = true,
            IsRandom = true
        };
    }

    public static CharacterSelectionResult Invalid(string errorMessage)
    {
        return new CharacterSelectionResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}