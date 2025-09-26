using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JinPingMei.Game.Hosting;
using JinPingMei.Game.Hosting.Commands;
using JinPingMei.Game.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;

namespace JinPingMei.Game;

public sealed class SpectreConsoleGame
{
    private readonly ILogger<SpectreConsoleGame> _logger;
    private readonly GameSession _gameSession;
    private readonly List<string> _commandHistory = new();
    private const int MaxCommandHistory = 50;

    public SpectreConsoleGame(
        ILogger<SpectreConsoleGame>? logger = null,
        IGameSessionFactory? sessionFactory = null)
    {
        _logger = logger ?? NullLogger<SpectreConsoleGame>.Instance;

        var factory = sessionFactory ?? new GameSessionFactory(
            new JsonLocalizationProvider(
                Path.Combine(AppContext.BaseDirectory, "Localization")),
            new NullDiagnostics());

        _gameSession = factory.Create();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Spectre.Console game session");

        try
        {
            // Clear console for a fresh start
            AnsiConsole.Clear();

            // Display welcome message
            DisplayWelcome();

            // Main game loop
            await RunGameLoopAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in game session");
            AnsiConsole.MarkupLine("[red]An error occurred. The game will now exit.[/]");
        }
        finally
        {
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
            _logger.LogInformation("Spectre.Console game session ended");
        }
    }

    private void DisplayWelcome()
    {
        AnsiConsole.WriteLine(_gameSession.RenderIntro());
        AnsiConsole.WriteLine();

        // Display a subtle hint
        AnsiConsole.MarkupLine("[dim]提示：輸入 [bold]/help[/] 查看所有指令，或 [bold]/commands[/] 查看指令分類[/]");
        AnsiConsole.WriteLine();
    }

    private async Task RunGameLoopAsync(CancellationToken cancellationToken)
    {
        var isFirstPrompt = true;
        var needsPromptSpacing = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Add spacing if needed
            if (!isFirstPrompt && needsPromptSpacing)
            {
                AnsiConsole.WriteLine();
            }
            isFirstPrompt = false;
            needsPromptSpacing = false;

            // Show contextual prompt and read user input with arrow key support
            var location = _gameSession.GetCurrentLocationName();
            var promptText = $"[dim]{location}[/] [bold green]>[/] ";

            // Use custom input handler for better Unicode/Chinese character handling and cursor movement
            var input = await Task.Run(() =>
            {
                try
                {
                    var inputHandler = new ConsoleInputHandler(promptText);
                    return inputHandler.ReadLine();
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch
                {
                    // Fallback to simple prompt if custom handler fails
                    AnsiConsole.Markup(promptText);
                    return Console.ReadLine();
                }
            }, cancellationToken);

            if (input is null || cancellationToken.IsCancellationRequested)
            {
                // End of input stream or cancelled
                break;
            }

            // Process the command
            var trimmedInput = input.Trim();

            if (string.IsNullOrEmpty(trimmedInput))
            {
                continue;
            }

            _logger.LogDebug("Processing input: {Input}", trimmedInput);

            // Add to command history
            if (_commandHistory.Count == 0 || !string.Equals(_commandHistory.Last(), trimmedInput, StringComparison.Ordinal))
            {
                if (_commandHistory.Count >= MaxCommandHistory)
                {
                    _commandHistory.RemoveAt(0);
                }
                _commandHistory.Add(trimmedInput);
            }

            // Handle special clear command
            if (trimmedInput.Equals("/clear", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.Clear();
                DisplayWelcome();
                continue;
            }

            // Handle display commands using Spectre.Console components
            if (trimmedInput.Equals("/look", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/l", StringComparison.OrdinalIgnoreCase))  // Allow shortcut
            {
                DisplayLook();
                needsPromptSpacing = true;
                continue;
            }

            if (trimmedInput.Equals("/status", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/s", StringComparison.OrdinalIgnoreCase))  // Allow shortcut
            {
                DisplayStatus();
                needsPromptSpacing = true;
                continue;
            }

            if (trimmedInput.Equals("/map", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/m", StringComparison.OrdinalIgnoreCase))  // Allow shortcut
            {
                DisplayMap();
                needsPromptSpacing = true;
                continue;
            }

            if (trimmedInput.Equals("/inventory", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/inv", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/i", StringComparison.OrdinalIgnoreCase))  // Allow shortcuts
            {
                DisplayInventory();
                needsPromptSpacing = true;
                continue;
            }

            if (trimmedInput.Equals("/commands", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/cmd", StringComparison.OrdinalIgnoreCase))  // Allow shortcut
            {
                DisplayCommands();
                needsPromptSpacing = true;
                continue;
            }

            CommandResult commandResult;
            try
            {
                commandResult = _gameSession.HandleInput(trimmedInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command: {Command}", trimmedInput);
                AnsiConsole.MarkupLine("[red]Error processing command. Please try again.[/]");
                needsPromptSpacing = true;
                continue;
            }

            // Display response (skip special display markers)
            foreach (var responseLine in commandResult.Lines)
            {
                if (!responseLine.StartsWith("[") || !responseLine.EndsWith("_DISPLAY]"))
                {
                    AnsiConsole.WriteLine(responseLine);
                }
            }

            if (commandResult.Lines.Count > 0)
            {
                needsPromptSpacing = true;
            }

            // Check if user wants to quit
            if (commandResult.ShouldDisconnect)
            {
                _logger.LogInformation("User requested to quit");
                break;
            }
        }
    }

    private void DisplayLook()
    {
        var snapshot = _gameSession.GetCurrentSceneSnapshot();
        var content = new StringBuilder();

        // Scene header with location
        content.AppendLine($"[bold cyan]{snapshot.SceneName}[/]");
        content.AppendLine($"[dim]{snapshot.LocaleName} • {snapshot.LocaleSummary}[/]");
        content.AppendLine();

        // Scene description
        content.AppendLine("[bold]場景描述[/]");
        content.AppendLine(snapshot.SceneDescription);
        content.AppendLine();

        // NPCs present
        content.AppendLine("[bold]在場人物[/]");
        if (snapshot.NpcNames.Count == 0)
        {
            content.AppendLine("[dim]  這裡沒有其他人[/]");
        }
        else
        {
            foreach (var npc in snapshot.NpcNames)
            {
                content.AppendLine($"  [yellow]•[/] {npc}");
            }
        }
        content.AppendLine();

        // Available exits
        content.AppendLine("[bold]可前往[/]");
        if (snapshot.Exits.Count == 0)
        {
            content.AppendLine("[dim]  沒有明顯的出口[/]");
        }
        else
        {
            foreach (var exit in snapshot.Exits)
            {
                content.Append($"  [green]→[/] {exit.DisplayName}");
                if (!string.IsNullOrWhiteSpace(exit.Description))
                {
                    content.Append($" [dim]({exit.Description})[/]");
                }
                content.AppendLine();
            }
        }

        // Create and display a Spectre.Console panel
        var panel = new Panel(content.ToString().TrimEnd())
        {
            Header = new PanelHeader("環境觀察"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0),
            Expand = false
        };

        AnsiConsole.Write(panel);
    }

    private void DisplayStatus()
    {
        var playerName = _gameSession.State.HasPlayerName
            ? _gameSession.State.PlayerName
            : "旅人";

        var snapshot = _gameSession.GetCurrentSceneSnapshot();

        // Create a table for player status
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn("Label")
            .AddColumn("Value");

        // Player information
        table.AddRow("[bold]角色名稱[/]", playerName);
        table.AddRow("[bold]當前位置[/]", $"{snapshot.LocaleName} › {snapshot.SceneName}");
        table.AddRow("[bold]遊戲時間[/]", $"第 {_commandHistory.Count} 回合");
        table.AddEmptyRow();

        // Game progress (placeholder for future features)
        table.AddRow("[bold]聲望[/]", "[dim]尚未實裝[/]");
        table.AddRow("[bold]銀兩[/]", "[dim]尚未實裝[/]");
        table.AddRow("[bold]物品數量[/]", "[dim]0 件[/]");

        // Create and display a Spectre.Console panel
        var panel = new Panel(table)
        {
            Header = new PanelHeader("玩家狀態"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0),
            Expand = false
        };

        AnsiConsole.Write(panel);

        // Add hint to use /look for surroundings
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]提示：使用 /look 查看周圍環境[/]");
    }

    private void DisplayMap()
    {
        var snapshot = _gameSession.GetCurrentSceneSnapshot();

        // Create a tree view for the map
        var tree = new Tree($"[bold yellow]{snapshot.LocaleName}[/]")
            .Style(Style.Parse("cyan"))
            .Guide(TreeGuide.Line);

        var currentNode = tree.AddNode($"[bold green]📍 {snapshot.SceneName}[/] [dim](當前位置)[/]");

        if (snapshot.Exits.Count > 0)
        {
            foreach (var exit in snapshot.Exits)
            {
                currentNode.AddNode($"[cyan]→[/] {exit.DisplayName}");
            }
        }
        else
        {
            currentNode.AddNode("[dim]沒有可前往的地方[/]");
        }

        var panel = new Panel(tree)
        {
            Header = new PanelHeader("地圖"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0),
            Expand = false
        };

        AnsiConsole.Write(panel);
    }

    private void DisplayInventory()
    {
        // For now, show a placeholder - will be expanded when inventory system is added
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[bold]物品[/]")
            .AddColumn("[bold]數量[/]")
            .AddColumn("[bold]描述[/]");

        // Placeholder items
        table.AddRow("[dim]空[/]", "[dim]-[/]", "[dim]你目前沒有攜帶任何物品[/]");

        var panel = new Panel(table)
        {
            Header = new PanelHeader("物品欄"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0),
            Expand = false
        };

        AnsiConsole.Write(panel);
    }

    private void DisplayCommands()
    {
        var grid = new Grid()
            .AddColumn()
            .AddColumn()
            .AddColumn();

        // Detailed command categories with full descriptions
        grid.AddRow(
            "[bold underline cyan]基本指令[/]",
            "[bold underline cyan]探索指令[/]",
            "[bold underline cyan]資訊指令[/]"
        );

        grid.AddRow(
            new Markup("[green]/help[/] [dim]([/][dim green]/h[/][dim])[/]\n" +
                      "  快速指令參考\n\n" +
                      "[green]/say <內容>[/]\n" +
                      "  對周圍說話\n\n" +
                      "[green]/name <名字>[/]\n" +
                      "  設定你的角色名稱"),
            new Markup("[yellow]/look[/] [dim]([/][dim yellow]/l[/][dim])[/]\n" +
                      "  查看場景描述、人物與出口\n\n" +
                      "[yellow]/go <地點>[/] [dim]([/][dim yellow]/g[/][dim])[/]\n" +
                      "  前往指定地點\n\n" +
                      "[yellow]/examine <目標>[/] [dim]([/][dim yellow]/ex[/][dim])[/]\n" +
                      "  仔細檢查人物或場景"),
            new Markup("[blue]/status[/] [dim]([/][dim blue]/s[/][dim])[/]\n" +
                      "  顯示玩家狀態與進度\n\n" +
                      "[blue]/map[/] [dim]([/][dim blue]/m[/][dim])[/]\n" +
                      "  顯示區域地圖結構\n\n" +
                      "[blue]/inventory[/] [dim]([/][dim blue]/i[/][dim])[/]\n" +
                      "  查看攜帶物品清單")
        );

        grid.AddEmptyRow();

        // System commands section
        grid.AddRow(
            "[bold underline cyan]系統指令[/]",
            "",
            "[bold underline cyan]中文快捷[/]"
        );

        grid.AddRow(
            new Markup("[magenta]/clear[/]\n" +
                      "  清除畫面重新開始\n\n" +
                      "[magenta]/commands[/] [dim]([/][dim magenta]/cmd[/][dim])[/]\n" +
                      "  顯示此詳細說明\n\n" +
                      "[red]/quit[/] [dim]([/][dim red]/q[/][dim])[/]\n" +
                      "  離開遊戲"),
            new Markup(""),
            new Markup("[dim]支援中文指令：[/]\n" +
                      "  看 → /look\n" +
                      "  去 → /go\n" +
                      "  說 → /say\n" +
                      "  狀態 → /status\n" +
                      "  地圖 → /map\n" +
                      "  物品 → /inventory\n" +
                      "  離開 → /quit")
        );

        var panel = new Panel(grid)
        {
            Header = new PanelHeader("完整指令說明"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0),
            Expand = false
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]提示：使用 /help 或 /h 查看快速指令參考[/]");
    }
}