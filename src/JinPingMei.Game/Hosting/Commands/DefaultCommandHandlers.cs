namespace JinPingMei.Game.Hosting.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    public string Command => "help";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        var lines = new[]
        {
            context.Localize("commands.help.summary"),
            context.Localize("commands.help.detail")
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
        return CommandResult.FromMessage(context.Localize("commands.look.description"));
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

public sealed class QuitCommandHandler : ICommandHandler
{
    public string Command => "quit";

    public CommandResult Handle(CommandContext context, string arguments)
    {
        return CommandResult.FromMessage(context.Localize("commands.quit.confirm"), shouldDisconnect: true);
    }
}
