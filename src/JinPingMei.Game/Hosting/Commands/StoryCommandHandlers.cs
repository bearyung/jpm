using System;
using System.Linq;

namespace JinPingMei.Game.Hosting.Commands;

public sealed class HostCommandHandler : ICommandHandler
{
    public string Command => "host";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        if (context.Story is null)
        {
            return CommandResult.FromMessage("目前尚未載入劇情模組。");
        }

        var roster = context.Story.HostRoster;
        if (roster.Count == 0)
        {
            return CommandResult.FromMessage("本卷未定義可選宿主。");
        }

        if (string.IsNullOrWhiteSpace(arguments))
        {
            var options = string.Join("、", roster);
            return CommandResult.FromMessage($"請輸入 /host <角色名稱>。可選：{options}");
        }

        var trimmed = arguments.Trim();
        var candidate = roster.FirstOrDefault(h => string.Equals(h, trimmed, StringComparison.OrdinalIgnoreCase));
        if (candidate is null)
        {
            var options = string.Join("、", roster);
            return CommandResult.FromMessage($"'{trimmed}' 不在可選列表。可選：{options}");
        }

        context.Story.SelectHost(candidate);
        context.Session.StoryHostId = candidate;
        return CommandResult.FromMessage($"已設定宿主為：{candidate}");
    }
}

public sealed class StoryStatusCommandHandler : ICommandHandler
{
    public string Command => "story";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        if (context.Story is null)
        {
            return CommandResult.FromMessage("目前尚未載入劇情模組。");
        }

        var status = context.Story.DescribeStatus();
        return CommandResult.FromMessage(status);
    }
}

public sealed class ProgressCommandHandler : ICommandHandler
{
    public string Command => "progress";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        if (context.Story is null)
        {
            return CommandResult.FromMessage("目前尚未載入劇情模組。");
        }

        var progress = context.Story.DescribeProgress();
        return CommandResult.FromMessage(progress);
    }
}
