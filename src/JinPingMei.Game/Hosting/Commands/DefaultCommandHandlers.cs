using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JinPingMei.Content.World;

namespace JinPingMei.Game.Hosting.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    public string Command => "help";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        // Quick reference - just the essential commands
        var lines = new List<string>
        {
            "快速指令參考：",
            "  /look (/l)     - 查看周圍環境",
            "  /go <地點> (/g) - 前往地點",
            "  /status (/s)   - 玩家狀態",
            "  /map (/m)      - 地圖與出口",
            "  /say <內容>    - 說話",
            "  /quit (/q)     - 離開遊戲",
            "",
            "輸入 /commands 查看完整指令分類說明"
        };

        return new CommandResult(lines, false);
    }
}

public sealed class NameCommandHandler : ICommandHandler
{
    private const int MaxNameLength = 32;

    public string Command => "name";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return CommandResult.FromMessage(context.Localize("commands.name.prompt"));
        }

        var trimmed = arguments.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            return CommandResult.FromMessage(context.Format("commands.name.too_long", MaxNameLength));
        }

        context.Session.PlayerName = trimmed;
        return CommandResult.FromMessage(context.Format("commands.name.confirm", trimmed));
    }
}

public sealed class LookCommandHandler : ICommandHandler
{
    public string Command => "look";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        // Return special marker for SpectreConsoleGame to render with Spectre.Console
        return CommandResult.FromMessage("[LOOK_DISPLAY]");
    }
}

public sealed class GoCommandHandler : ICommandHandler
{
    public string Command => "go";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            // Return special marker for SpectreConsoleGame to show SelectionPrompt
            return CommandResult.FromMessage("[GO_SELECT_DISPLAY]");
        }

        if (context.World.TryMove(arguments, out var exit))
        {
            context.Session.CurrentLocaleId = context.World.CurrentLocale.Id;
            context.Session.CurrentSceneId = context.World.CurrentScene.Id;

            var lines = WorldCommandFormatter.BuildArrivalLines(context, exit);
            return new CommandResult(lines, false);
        }

        return CommandResult.FromMessage(context.Localize("commands.go.unknown"));
    }
}

public sealed class ExamineCommandHandler : ICommandHandler
{
    private static readonly string[] SceneKeywords = { "scene", "場景", "地點", "環境" };

    public string Command => "examine";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return CommandResult.FromMessage(context.Localize("commands.examine.prompt"));
        }

        var normalized = arguments.Trim();
        if (SceneKeywords.Any(keyword => keyword.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            var lines = WorldCommandFormatter.BuildExamineSceneLines(context);
            return new CommandResult(lines, false);
        }

        if (context.World.TryFindNpc(normalized, out var npc))
        {
            var lines = WorldCommandFormatter.BuildExamineNpcLines(context, npc);
            return new CommandResult(lines, false);
        }

        return CommandResult.FromMessage(context.Localize("commands.examine.unknown"));
    }
}

public sealed class SayCommandHandler : ICommandHandler
{
    public string Command => "say";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return CommandResult.FromMessage(context.Localize("commands.say.prompt"));
        }

        var displayName = context.Session.HasPlayerName
            ? context.Session.PlayerName!
            : context.Localize("session.display_name.default");

        return CommandResult.FromMessage(context.Format("commands.say.echo", displayName, arguments.Trim()));
    }
}

public sealed class StatusCommandHandler : ICommandHandler
{
    public string Command => "status";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        // This handler now returns a special marker to indicate status should be displayed
        // The actual rendering will be done by SpectreConsoleGame using Spectre.Console components
        return CommandResult.FromMessage("[STATUS_DISPLAY]");
    }
}

public sealed class MapCommandHandler : ICommandHandler
{
    public string Command => "map";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        // Return special marker for SpectreConsoleGame to render with Spectre.Console
        return CommandResult.FromMessage("[MAP_DISPLAY]");
    }
}

public sealed class InventoryCommandHandler : ICommandHandler
{
    public string Command => "inventory";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        // Return special marker for SpectreConsoleGame to render with Spectre.Console
        return CommandResult.FromMessage("[INVENTORY_DISPLAY]");
    }
}

public sealed class CommandsCommandHandler : ICommandHandler
{
    public string Command => "commands";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        // Return special marker for SpectreConsoleGame to render detailed command categories
        return CommandResult.FromMessage("[COMMANDS_DISPLAY]");
    }
}

public sealed class QuitCommandHandler : ICommandHandler
{
    public string Command => "quit";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        return CommandResult.FromMessage(context.Localize("commands.quit.confirm"), shouldDisconnect: true);
    }
}

internal static class WorldCommandFormatter
{
    public static IReadOnlyList<string> BuildLookLines(CommandContext context)
    {
        return BuildOverviewLines(context, includeHeader: true);
    }

    public static IReadOnlyList<string> BuildArrivalLines(CommandContext context, SceneExitDefinition exit)
    {
        var lines = new List<string>
        {
            context.Format("commands.go.transition", exit.DisplayName, context.World.CurrentScene.Name)
        };
        lines.AddRange(BuildOverviewLines(context, includeHeader: false));
        return lines;
    }

    public static IReadOnlyList<string> BuildExamineSceneLines(CommandContext context)
    {
        var lines = new List<string>
        {
            context.Format("commands.examine.scene", context.World.CurrentScene.Name),
            context.Format("commands.look.locale", context.World.CurrentLocale.Name, context.World.CurrentLocale.Summary),
            context.World.CurrentScene.Description
        };

        AppendExitDetails(context, lines);
        AppendNpcPresence(context, lines, detailed: true);

        return lines;
    }

    public static IReadOnlyList<string> BuildExamineNpcLines(CommandContext context, NpcDefinition npc)
    {
        var lines = new List<string>
        {
            context.Format("commands.examine.npc", npc.Name),
            npc.Description
        };

        return lines;
    }

    private static IReadOnlyList<string> BuildOverviewLines(CommandContext context, bool includeHeader)
    {
        var lines = new List<string>();

        if (includeHeader)
        {
            lines.Add(context.Format("commands.look.header", context.World.CurrentScene.Name));
        }

        lines.Add(context.Format("commands.look.locale", context.World.CurrentLocale.Name, context.World.CurrentLocale.Summary));
        lines.Add(context.World.CurrentScene.Description);

        AppendNpcPresence(context, lines, detailed: false);
        AppendExitSummary(context, lines);

        return lines;
    }

    private static void AppendNpcPresence(CommandContext context, List<string> lines, bool detailed)
    {
        if (context.World.Npcs.Count == 0)
        {
            lines.Add(context.Localize("commands.look.no_npcs"));
            return;
        }

        var names = string.Join("、", context.World.Npcs.Select(n => n.Name));
        lines.Add(context.Format("commands.look.npcs", names));

        if (detailed)
        {
            foreach (var npc in context.World.Npcs)
            {
                lines.Add(context.Format("commands.examine.npc_hint", npc.Name, npc.Description));
            }
        }
    }

    private static void AppendExitSummary(CommandContext context, List<string> lines)
    {
        if (context.World.Exits.Count == 0)
        {
            lines.Add(context.Localize("commands.look.no_exits"));
            return;
        }

        var exits = string.Join("、", context.World.Exits.Select(e => e.DisplayName));
        lines.Add(context.Format("commands.look.exits", exits));
    }

    private static void AppendExitDetails(CommandContext context, List<string> lines)
    {
        if (context.World.Exits.Count == 0)
        {
            lines.Add(context.Localize("commands.look.no_exits"));
            return;
        }

        lines.Add(context.Localize("commands.examine.paths"));
        foreach (var exit in context.World.Exits)
        {
            lines.Add(context.Format("commands.examine.path_detail", exit.DisplayName, exit.Description));
        }
    }
}
