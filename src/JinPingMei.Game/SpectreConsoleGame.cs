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
    private const int MaxTranscriptLines = 800;
    private const int MaxCommandHistory = 50;

    private readonly GameSession _gameSession;
    private readonly List<string> _transcript = new();
    private readonly List<string> _commandHistory = new();
    // private int _historyCursor = -1; // TODO: implement command history

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
        // Set up the console
        AnsiConsole.Clear();

        // Initialize the game
        InitializeTranscript();

        // Main game loop
        await RunGameLoopAsync(cancellationToken);
    }

    private async Task RunGameLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Clear and redraw the entire UI
            AnsiConsole.Clear();

            // Draw the UI layout
            DrawUI();

            // Get user input
            var input = await GetUserInputAsync(cancellationToken);

            if (input == null || cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Process the command
            if (!string.IsNullOrWhiteSpace(input))
            {
                ProcessCommand(input);
            }
        }

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[yellow]遊戲已結束。感謝遊玩！[/]");
    }

    private void DrawUI()
    {
        // Create the main layout
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(3),
                new Layout("Main").Ratio(1),
                new Layout("Footer").Size(3)
            );

        // Configure header
        layout["Header"].Update(
            new Panel(
                new Text("金瓶梅 JinPingMei - 互動敘事實驗", new Style(Color.Yellow, Color.Blue))
                    .Centered()
            ).Border(BoxBorder.Double)
        );

        // Split main area into transcript and info panels
        layout["Main"].SplitColumns(
            new Layout("Transcript").Ratio(2),
            new Layout("Info").Ratio(1)
        );

        // Add transcript panel
        var transcriptContent = BuildTranscriptContent();
        layout["Transcript"].Update(
            new Panel(transcriptContent)
                .Header("[bold yellow]事件紀錄[/]")
                .Border(BoxBorder.Rounded)
        );

        // Add info panel
        var infoContent = BuildInfoContent();
        layout["Info"].Update(
            new Panel(infoContent)
                .Header("[bold cyan]當前狀態[/]")
                .Border(BoxBorder.Rounded)
        );

        // Add footer with hints
        layout["Footer"].Update(
            new Panel(
                new Text("指令: /help (說明) | /look (查看) | /quit (離開) | Ctrl+C (強制結束)")
                    .Centered()
            ).Border(BoxBorder.Rounded)
        );

        // Render the layout
        AnsiConsole.Write(layout);
    }

    private Markup BuildTranscriptContent()
    {
        var content = new StringBuilder();

        // Show recent transcript lines (last N lines that fit the screen)
        var linesToShow = Math.Min(_transcript.Count, 20); // Adjust based on terminal size
        var startIndex = Math.Max(0, _transcript.Count - linesToShow);

        for (int i = startIndex; i < _transcript.Count; i++)
        {
            var line = Markup.Escape(_transcript[i]);
            if (line.StartsWith(">"))
            {
                content.AppendLine($"[bold green]{line}[/]");
            }
            else
            {
                content.AppendLine(line);
            }
        }

        if (content.Length == 0)
        {
            content.AppendLine("[dim]（暫無事件）[/]");
        }

        return new Markup(content.ToString());
    }

    private Markup BuildInfoContent()
    {
        var snapshot = _gameSession.GetCurrentSceneSnapshot();
        var content = new StringBuilder();

        content.AppendLine($"[bold]位置:[/] {Markup.Escape(snapshot.LocaleName)} › {Markup.Escape(snapshot.SceneName)}");
        content.AppendLine();

        if (!string.IsNullOrWhiteSpace(snapshot.LocaleSummary))
        {
            content.AppendLine($"[dim]{Markup.Escape(snapshot.LocaleSummary)}[/]");
            content.AppendLine();
        }

        content.AppendLine($"{Markup.Escape(snapshot.SceneDescription)}");
        content.AppendLine();

        content.AppendLine("[bold]在場角色:[/]");
        if (snapshot.NpcNames.Count == 0)
        {
            content.AppendLine("  [dim]（暫無）[/]");
        }
        else
        {
            foreach (var npc in snapshot.NpcNames)
            {
                content.AppendLine($"  • {Markup.Escape(npc)}");
            }
        }

        content.AppendLine();
        content.AppendLine("[bold]可前往:[/]");
        if (snapshot.Exits.Count == 0)
        {
            content.AppendLine("  [dim]（暫無）[/]");
        }
        else
        {
            foreach (var exit in snapshot.Exits)
            {
                content.AppendLine($"  → {Markup.Escape(exit.DisplayName)}");
                if (!string.IsNullOrWhiteSpace(exit.Description))
                {
                    content.AppendLine($"     [dim]{Markup.Escape(exit.Description)}[/]");
                }
            }
        }

        var playerName = _gameSession.State.HasPlayerName
            ? _gameSession.State.PlayerName
            : "旅人";

        content.AppendLine();
        content.AppendLine($"[bold]玩家:[/] {Markup.Escape(playerName ?? "旅人")}");

        return new Markup(content.ToString());
    }

    private async Task<string?> GetUserInputAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Simple console input for now
            // Spectre.Console prompts don't work well in async/interactive scenarios
            AnsiConsole.Markup("[bold yellow]>[/] ");

            var input = await Task.Run(() =>
            {
                try
                {
                    return Console.ReadLine();
                }
                catch
                {
                    return null;
                }
            }, cancellationToken);

            return input;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private void ProcessCommand(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        AppendToTranscript($"> {trimmed}");

        // Add to command history
        if (_commandHistory.Count == 0 || !string.Equals(_commandHistory.Last(), trimmed, StringComparison.Ordinal))
        {
            if (_commandHistory.Count >= MaxCommandHistory)
            {
                _commandHistory.RemoveAt(0);
            }
            _commandHistory.Add(trimmed);
        }

        // Handle special commands
        if (trimmed.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            Environment.Exit(0);
        }

        if (trimmed.Equals("/clear", StringComparison.OrdinalIgnoreCase))
        {
            _transcript.Clear();
            AppendToTranscript("（紀錄已清除，旅程繼續。）");
            return;
        }

        // Process game command
        CommandResult result;
        try
        {
            result = _gameSession.HandleInput(trimmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing input '{Input}'", trimmed);
            AppendToTranscript("Error processing command. 請再試一次。");
            return;
        }

        foreach (var line in result.Lines)
        {
            AppendToTranscript(line);
        }

        if (result.ShouldDisconnect)
        {
            Environment.Exit(0);
        }
    }

    private void InitializeTranscript()
    {
        AppendToTranscript("《金瓶梅》互動敘事實驗已啟動。");
        AppendToTranscript("旅途中可隨時輸入 /help 取得指令說明。");
        AppendToTranscript("");
        AppendToTranscript("歡迎來到《金瓶梅》互動敘事實驗。");
        AppendToTranscript(" 輸入 '/help' 查看指令清單，輸入 '/quit' 離線。");
        AppendToTranscript("");
        AppendToTranscript(_gameSession.RenderIntro());
        AppendToTranscript(_gameSession.GetCommandHint());
        AppendToTranscript("");
    }

    private void AppendToTranscript(string text)
    {
        _transcript.Add(text);

        // Trim old lines if needed
        if (_transcript.Count > MaxTranscriptLines)
        {
            var overflow = _transcript.Count - MaxTranscriptLines;
            _transcript.RemoveRange(0, overflow);
        }
    }
}