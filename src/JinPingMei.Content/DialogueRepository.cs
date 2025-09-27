using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using JinPingMei.Content.Dialogue;

namespace JinPingMei.Content;

public sealed class DialogueRepository
{
    private const string DefaultDialogueFileName = "dialogues.zh-TW.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly Lazy<DialogueDefinition> _dialogues;

    public DialogueRepository()
    {
        _dialogues = new Lazy<DialogueDefinition>(LoadDialoguesCore);
    }

    public DialogueDefinition LoadDialogues() => _dialogues.Value;

    private static DialogueDefinition LoadDialoguesCore()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "Data", DefaultDialogueFileName),
            Path.Combine(baseDirectory, DefaultDialogueFileName)
        };

        var dialoguePath = candidates.FirstOrDefault(File.Exists);
        if (dialoguePath is not null)
        {
            var jsonFromFile = File.ReadAllText(dialoguePath);
            return ParseDialogues(jsonFromFile);
        }

        var assembly = typeof(DialogueRepository).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(DefaultDialogueFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            // Return empty definition if no dialogue file found
            return new DialogueDefinition
            {
                DefaultLocaleId = string.Empty,
                DialogueSets = Array.Empty<DialogueSet>()
            };
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
                          ?? throw new InvalidOperationException($"Embedded dialogue resource '{resourceName}' could not be opened.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return ParseDialogues(json);
    }

    private static DialogueDefinition ParseDialogues(string json)
    {
        var definition = JsonSerializer.Deserialize<DialogueDefinition>(json, JsonOptions)
                        ?? throw new InvalidOperationException("Dialogue definition payload was empty.");

        ValidateDialogues(definition);
        return definition;
    }

    private static void ValidateDialogues(DialogueDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.DefaultLocaleId))
        {
            throw new InvalidOperationException("Dialogue definition requires a default locale ID.");
        }

        foreach (var dialogueSet in definition.DialogueSets)
        {
            if (string.IsNullOrWhiteSpace(dialogueSet.Id))
            {
                throw new InvalidOperationException("All dialogue sets must have an ID.");
            }

            if (string.IsNullOrWhiteSpace(dialogueSet.NpcId))
            {
                throw new InvalidOperationException($"Dialogue set '{dialogueSet.Id}' must specify an NPC ID.");
            }

            foreach (var entry in dialogueSet.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    throw new InvalidOperationException($"All dialogue entries in set '{dialogueSet.Id}' must have an ID.");
                }

                if (entry.Lines.Count == 0)
                {
                    throw new InvalidOperationException($"Dialogue entry '{entry.Id}' in set '{dialogueSet.Id}' must have at least one line.");
                }
            }
        }
    }
}