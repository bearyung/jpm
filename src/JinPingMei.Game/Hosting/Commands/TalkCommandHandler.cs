using System.Collections.Generic;
using System.Linq;
using JinPingMei.Engine.Dialogue;

namespace JinPingMei.Game.Hosting.Commands;

public sealed class TalkCommandHandler : ICommandHandler
{
    public string Command => "talk";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            // Return special marker for SpectreConsoleGame to show NPC selection prompt
            return CommandResult.FromMessage("[TALK_SELECT_DISPLAY]");
        }

        var normalized = arguments.Trim();

        // Try to find the NPC in the current scene
        if (!context.World.TryFindNpc(normalized, out var npc))
        {
            return CommandResult.FromMessage(context.Localize("commands.talk.unknown"));
        }

        // Create dialogue context
        var dialogueContext = new DialogueContext(
            currentLocaleId: context.World.CurrentLocale.Id,
            currentSceneId: context.World.CurrentScene.Id,
            currentHost: context.Session.HasStoryHost ? context.Session.StoryHost : null,
            inventory: GetInventory(context),
            itemGrantCallback: item => HandleItemGrant(context, item),
            eventTriggerCallback: eventId => HandleEventTrigger(context, eventId))
        {
            ChapterProgress = context.Session.ChapterProgress,
            TimeOfDay = GetTimeOfDay(context)
        };

        // Get dialogue from the dialogue manager
        var dialogueResult = context.Runtime.DialogueManager.GetDialogue(npc.Id, dialogueContext);

        if (dialogueResult == null || dialogueResult.Lines.Count == 0)
        {
            // Fallback to NPC description if no dialogue available
            var fallbackLines = new List<string>
            {
                context.Format("commands.talk.approach", npc.Name),
                "",
                npc.Description
            };
            return new CommandResult(fallbackLines, false);
        }

        // Build result lines
        var lines = new List<string>
        {
            context.Format("commands.talk.approach", npc.Name),
            ""
        };
        lines.AddRange(dialogueResult.Lines);

        // If there are player response options, we could show them here
        // For now, we'll just show the dialogue
        if (dialogueResult.Responses.Count > 0)
        {
            lines.Add("");
            lines.Add("[dim]（對話選項尚未實現）[/]");
        }

        return new CommandResult(lines, false);
    }

    private static IEnumerable<string> GetInventory(CommandContext context)
    {
        // TODO: Implement inventory system
        // For now, return empty
        return Enumerable.Empty<string>();
    }

    private static void HandleItemGrant(CommandContext context, string item)
    {
        // TODO: Implement item granting
        // For now, just log it or add to future inventory system
    }

    private static void HandleEventTrigger(CommandContext context, string eventId)
    {
        // TODO: Implement event system
        // For now, just track it in session state or trigger story events
    }

    private static string GetTimeOfDay(CommandContext context)
    {
        // TODO: Implement time system
        // For now, return default "day"
        return "day";
    }
}