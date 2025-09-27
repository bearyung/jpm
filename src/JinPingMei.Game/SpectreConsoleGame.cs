using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JinPingMei.Engine.Story;
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
                trimmedInput.Equals("/l", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("看"))  // Chinese shortcut
            {
                DisplayLook();
                needsPromptSpacing = true;
                continue;
            }

            if (trimmedInput.Equals("/status", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/s", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("狀態"))  // Chinese shortcut
            {
                DisplayStatus();
                needsPromptSpacing = true;
                continue;
            }

            if (trimmedInput.Equals("/map", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/m", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("地圖"))  // Chinese shortcut
            {
                DisplayMap();
                needsPromptSpacing = true;
                continue;
            }

            if (trimmedInput.Equals("/inventory", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/inv", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/i", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("物品"))  // Chinese shortcut
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

            if (trimmedInput.Equals("/progress", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/p", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("進度"))  // Chinese shortcut
            {
                DisplayProgress();
                needsPromptSpacing = true;
                continue;
            }

            if (trimmedInput.Equals("/progress detail", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("/pd", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.Equals("進度詳情"))  // Chinese shortcut
            {
                DisplayProgressDetail();
                needsPromptSpacing = true;
                continue;
            }

            // Convert Chinese shortcuts to their command equivalents
            var processedInput = ConvertChineseShortcut(trimmedInput);

            CommandResult commandResult;
            try
            {
                commandResult = _gameSession.HandleInput(processedInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command: {Command}", trimmedInput);
                AnsiConsole.MarkupLine("[red]Error processing command. Please try again.[/]");
                needsPromptSpacing = true;
                continue;
            }

            // Check for special display markers
            if (commandResult.Lines.Count == 1)
            {
                if (commandResult.Lines[0] == "[GO_SELECT_DISPLAY]")
                {
                    HandleGoSelection();
                    needsPromptSpacing = true;
                    continue;
                }
                else if (commandResult.Lines[0] == "[HELP_DISPLAY]")
                {
                    DisplayHelp();
                    needsPromptSpacing = true;
                    continue;
                }
                else if (commandResult.Lines[0] == "[QUIT_CONFIRM_DISPLAY]")
                {
                    if (HandleQuitConfirmation())
                    {
                        break; // Exit the game loop
                    }
                    needsPromptSpacing = true;
                    continue;
                }
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

        // Scene header with location
        AnsiConsole.MarkupLine($"[bold cyan]{snapshot.SceneName}[/]");
        AnsiConsole.MarkupLine($"[dim]{snapshot.LocaleName} • {snapshot.LocaleSummary}[/]");
        AnsiConsole.WriteLine();

        // Scene description
        AnsiConsole.MarkupLine("[bold]場景描述[/]");
        AnsiConsole.WriteLine(snapshot.SceneDescription);
        AnsiConsole.WriteLine();

        // NPCs present
        AnsiConsole.MarkupLine("[bold]在場人物[/]");
        if (snapshot.NpcNames.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  這裡沒有其他人[/]");
        }
        else
        {
            foreach (var npc in snapshot.NpcNames)
            {
                AnsiConsole.MarkupLine($"  [yellow]•[/] {npc}");
            }
        }
        AnsiConsole.WriteLine();

        // Available exits
        AnsiConsole.MarkupLine("[bold]可前往[/]");
        if (snapshot.Exits.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  沒有明顯的出口[/]");
        }
        else
        {
            foreach (var exit in snapshot.Exits)
            {
                var exitLine = $"  [green]→[/] {exit.DisplayName}";
                if (!string.IsNullOrWhiteSpace(exit.Description))
                {
                    exitLine += $" [dim]({exit.Description})[/]";
                }
                AnsiConsole.MarkupLine(exitLine);
            }
        }
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

    private void HandleGoSelection()
    {
        var snapshot = _gameSession.GetCurrentSceneSnapshot();

        // Check if there are any exits available
        if (snapshot.Exits.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]這個位置沒有可前往的地點。[/]");
            return;
        }

        // Create selection prompt with available exits
        var prompt = new SelectionPrompt<string>()
            .Title("[bold cyan]請選擇要前往的地點：[/]")
            .PageSize(10)
            .MoreChoicesText("[dim](使用上下方向鍵移動，Enter 選擇)[/]");

        // Add cancel option first
        prompt.AddChoice("[red]取消[/]");

        // Add each available exit as a choice
        foreach (var exit in snapshot.Exits)
        {
            var choice = exit.DisplayName;
            if (!string.IsNullOrWhiteSpace(exit.Description))
            {
                choice += $" [dim]({exit.Description})[/]";
            }
            prompt.AddChoice(choice);
        }

        // Show the prompt and get selection
        var selection = AnsiConsole.Prompt(prompt);

        // Handle the selection
        if (selection == "[red]取消[/]")
        {
            AnsiConsole.MarkupLine("[dim]已取消前往。[/]");
            return;
        }

        // Extract the actual location name (remove description if present)
        var locationName = selection;
        var descriptionIndex = locationName.IndexOf(" [dim](");
        if (descriptionIndex > 0)
        {
            locationName = locationName.Substring(0, descriptionIndex);
        }

        // Execute the go command with the selected location
        var goCommand = $"/go {locationName}";
        var commandResult = _gameSession.HandleInput(goCommand);

        // Display the result
        foreach (var line in commandResult.Lines)
        {
            AnsiConsole.WriteLine(line);
        }
    }

    private bool HandleQuitConfirmation()
    {
        // Create confirmation prompt
        var prompt = new SelectionPrompt<string>()
            .Title("[bold yellow]確定要離開遊戲嗎？[/]")
            .PageSize(5)
            .MoreChoicesText("[dim](使用上下方向鍵移動，Enter 選擇)[/]");

        // Add options with clear visual distinction
        prompt.AddChoice("[red]是的，離開遊戲[/]");
        prompt.AddChoice("[green]不，繼續遊戲[/]");

        // Show the prompt and get selection
        var selection = AnsiConsole.Prompt(prompt);

        // Handle the selection
        if (selection == "[red]是的，離開遊戲[/]")
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]感謝遊玩，再見！[/]");
            return true; // Confirm quit
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]已取消離開。[/]");
            return false; // Cancel quit
        }
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

    private void DisplayProgress()
    {
        // Check if story is loaded
        var story = _gameSession.Story;
        if (story is null)
        {
            AnsiConsole.MarkupLine("[red]尚未載入劇情模組。[/]");
            return;
        }

        // Get progress data from story
        var progressData = story.GetProgressData();

        // Show inline progress info that stays in conversation
        DisplayProgressInfo(progressData);
    }

    private void DisplayProgressInfo(StoryProgressData progressData)
    {
        // Create a compact inline display
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold cyan]【進度】[/] {progressData.VolumeTitle} • 第{progressData.CurrentChapterNumber}回 ({progressData.CurrentChapterIndex + 1}/{progressData.TotalChapters})");
        AnsiConsole.MarkupLine($"[bold cyan]【任務】[/] 完成 {progressData.CompletedMissions}/{progressData.TotalMissions} ([yellow]{progressData.OverallProgress}%[/])");

        // Show current scene if available
        if (progressData.CurrentSceneTitle != null)
        {
            AnsiConsole.MarkupLine($"[bold cyan]【場景】[/] {progressData.CurrentSceneTitle} ({progressData.CurrentSceneIndex + 1}/{progressData.TotalScenesInChapter})");
        }

        // Quick mission summary
        if (progressData.CurrentChapterMissions?.Count > 0)
        {
            var inProgress = progressData.CurrentChapterMissions.Count(m => m.Status == MissionStatus.InProgress);
            var locked = progressData.CurrentChapterMissions.Count(m => m.Status == MissionStatus.Locked);
            var completed = progressData.CurrentChapterMissions.Count(m => m.Status == MissionStatus.Completed);

            AnsiConsole.MarkupLine($"[dim]       進行中: {inProgress} | 已完成: {completed} | 未解鎖: {locked}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]輸入 [bold]/progress detail[/] 或 [bold]/pd[/] 查看完整任務列表[/]");
    }

    private void DisplayProgressDetail()
    {
        // Check if story is loaded
        var story = _gameSession.Story;
        if (story is null)
        {
            AnsiConsole.MarkupLine("[red]尚未載入劇情模組。[/]");
            return;
        }

        // Get progress data and display in waterfall format
        var progressData = story.GetProgressData();
        DisplayProgressDetailWaterfall(progressData);
    }

    private void DisplayProgressDetailWaterfall(StoryProgressData progressData)
    {
        AnsiConsole.WriteLine();

        // Section 1: Volume and Episode Info
        AnsiConsole.MarkupLine("[bold cyan]【當前進度】[/]");
        AnsiConsole.MarkupLine($"[bold]{progressData.VolumeTitle}[/] - [dim]{progressData.EpisodeLabel}[/]");
        AnsiConsole.MarkupLine($"宿主：[yellow]{progressData.HostId ?? "未選擇"}[/]");
        AnsiConsole.WriteLine();

        // Section 2: Chapter Progress
        AnsiConsole.MarkupLine("[bold cyan]【章節資訊】[/]");
        if (progressData.CurrentChapterNumber > 0)
        {
            AnsiConsole.MarkupLine($"[bold]第{progressData.CurrentChapterNumber}回[/]");
            if (progressData.ChapterTitles?.Count > 0)
            {
                foreach (var title in progressData.ChapterTitles)
                {
                    AnsiConsole.MarkupLine($"  [italic]{title}[/]");
                }
            }
            AnsiConsole.MarkupLine($"進度：第 {progressData.CurrentChapterIndex + 1} 章 / 共 {progressData.TotalChapters} 章");

            if (progressData.CurrentSceneTitle != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]當前場景：[/]{progressData.CurrentSceneTitle} ({progressData.CurrentSceneIndex + 1}/{progressData.TotalScenesInChapter})");
                if (progressData.CurrentBeatProgress != null)
                {
                    AnsiConsole.MarkupLine($"節拍進度：{progressData.CurrentBeatProgress}");
                }
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]尚未開始[/]");
        }
        AnsiConsole.WriteLine();

        // Section 3: Mission Progress
        AnsiConsole.MarkupLine("[bold cyan]【任務狀態】[/]");

        if (progressData.CurrentChapterMissions?.Count > 0)
        {
            foreach (var mission in progressData.CurrentChapterMissions)
            {
                var statusIcon = mission.Status switch
                {
                    MissionStatus.Locked => "[red]🔒[/]",
                    MissionStatus.InProgress => "[yellow]○[/]",
                    MissionStatus.Completed => "[green]✓[/]",
                    _ => "[dim]?[/]"
                };

                var titleMarkup = mission.Status switch
                {
                    MissionStatus.Locked => $"[dim strikethrough]{mission.Title}[/]",
                    MissionStatus.InProgress => $"[bold yellow]{mission.Title}[/]",
                    MissionStatus.Completed => $"[green strikethrough]{mission.Title}[/]",
                    _ => mission.Title
                };

                AnsiConsole.MarkupLine($"  {statusIcon} {titleMarkup}");

                if (mission.Status == MissionStatus.InProgress && !string.IsNullOrWhiteSpace(mission.Description))
                {
                    AnsiConsole.MarkupLine($"     [dim italic]{mission.Description}[/]");
                }
            }
        }
        else
        {
            AnsiConsole.MarkupLine("  [dim]無任務[/]");
        }
        AnsiConsole.WriteLine();

        // Section 4: Overall Statistics
        AnsiConsole.MarkupLine("[bold cyan]【總體統計】[/]");
        AnsiConsole.MarkupLine($"總任務進度：{progressData.CompletedMissions}/{progressData.TotalMissions} ([yellow]{progressData.OverallProgress}%[/])");

        if (progressData.CurrentChapterMissions?.Count > 0)
        {
            var inProgress = progressData.CurrentChapterMissions.Count(m => m.Status == MissionStatus.InProgress);
            var locked = progressData.CurrentChapterMissions.Count(m => m.Status == MissionStatus.Locked);
            var completed = progressData.CurrentChapterMissions.Count(m => m.Status == MissionStatus.Completed);

            AnsiConsole.MarkupLine($"[dim]進行中: {inProgress} | 已完成: {completed} | 未解鎖: {locked}[/]");
        }
    }

    private static void WaitForQuitKey()
    {
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)
            {
                break;
            }
        }
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

        // Story and Progress commands
        grid.AddRow(
            "[bold underline cyan]劇情與進度[/]",
            "",
            ""
        );

        grid.AddRow(
            new Markup("[cyan]/host <角色>[/]\n" +
                      "  選擇故事宿主\n\n" +
                      "[cyan]/story[/]\n" +
                      "  查看故事狀態"),
            new Markup("[cyan]/progress[/] [dim]([/][dim cyan]/p[/][dim])[/]\n" +
                      "  查看進度摘要\n\n" +
                      "[cyan]/progress detail[/] [dim]([/][dim cyan]/pd[/][dim])[/]\n" +
                      "  查看詳細任務列表"),
            new Markup("")
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
                      "  進度 → /progress\n" +
                      "  進度詳情 → /pd\n" +
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

    private void DisplayHelp()
    {
        // Display CLI-style help in a format similar to Unix man pages
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold underline]JINGPINGMEI(1)[/]                    User Commands                    [bold underline]JINGPINGMEI(1)[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]NAME[/]");
        AnsiConsole.MarkupLine("       金瓶梅 - 互動式文字冒險遊戲");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]SYNOPSIS[/]");
        AnsiConsole.MarkupLine("       [cyan]/command[/] [dim][[options]][/] [dim][[arguments]][/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]DESCRIPTION[/]");
        AnsiConsole.MarkupLine("       在明朝清河城中體驗經典故事，透過指令進行遊戲。");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]ESSENTIAL COMMANDS[/]");

        // Create a table for commands with better alignment
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Command").Width(20))
            .AddColumn(new TableColumn("Alias").Width(15))
            .AddColumn(new TableColumn("Description"));

        table.AddRow("[cyan]/look[/]", "[dim]l, 看[/]", "查看周圍環境");
        table.AddRow("[cyan]/go[/] [dim]<location>[/]", "[dim]g, 去[/]", "前往指定地點（無參數時顯示選單）");
        table.AddRow("[cyan]/status[/]", "[dim]s, 狀態[/]", "查看玩家狀態");
        table.AddRow("[cyan]/map[/]", "[dim]m, 地圖[/]", "顯示地圖與可用出口");
        table.AddRow("[cyan]/inventory[/]", "[dim]i, inv, 物品[/]", "查看物品欄");
        table.AddRow("[cyan]/say[/] [dim]<text>[/]", "[dim]說[/]", "說出對話");
        table.AddRow("[cyan]/examine[/] [dim]<target>[/]", "[dim]x, 檢查[/]", "仔細檢查目標");
        table.AddEmptyRow();
        table.AddRow("[cyan]/host[/] [dim]<character>[/]", "[dim]宿主[/]", "選擇故事宿主角色");
        table.AddRow("[cyan]/progress[/]", "[dim]p, 進度[/]", "顯示進度摘要");
        table.AddRow("[cyan]/progress detail[/]", "[dim]pd, 進度詳情[/]", "顯示詳細任務列表");
        table.AddEmptyRow();
        table.AddRow("[cyan]/commands[/]", "[dim]cmd[/]", "顯示完整指令分類");
        table.AddRow("[cyan]/help[/]", "[dim]h, ?[/]", "顯示此說明");
        table.AddRow("[cyan]/quit[/]", "[dim]q, 離開[/]", "離開遊戲");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]EXAMPLES[/]");
        AnsiConsole.MarkupLine("       [cyan]/go[/]          # 顯示可前往地點的選單");
        AnsiConsole.MarkupLine("       [cyan]/go 西廂房[/]   # 直接前往西廂房");
        AnsiConsole.MarkupLine("       [cyan]/pd[/]          # 查看當前章節的任務進度");
        AnsiConsole.MarkupLine("       [cyan]去[/]            # 中文快捷，等同於 /go");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]SEE ALSO[/]");
        AnsiConsole.MarkupLine("       輸入 [cyan]/commands[/] 查看按類別組織的完整指令說明");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]Jin Ping Mei Game v1.0                     2024                    JINGPINGMEI(1)[/]");
    }

    private static string ConvertChineseShortcut(string input)
    {
        // Convert Chinese commands to their /command equivalents
        // Handle both standalone commands and commands with arguments
        if (input.StartsWith("去"))
        {
            return input.Length > 1 ? $"/go{input[1..]}" : "/go";
        }
        if (input.StartsWith("說"))
        {
            return input.Length > 1 ? $"/say{input[1..]}" : "/say";
        }
        if (input.Equals("離開"))
        {
            return "/quit";
        }
        if (input.Equals("進度"))
        {
            return "/progress";
        }

        return input;
    }
}