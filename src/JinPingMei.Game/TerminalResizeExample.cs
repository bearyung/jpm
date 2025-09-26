using System;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace JinPingMei.Game;

/// <summary>
/// Example implementation of terminal resize detection for Spectre.Console
/// </summary>
public class TerminalResizeExample
{
    private int _lastWidth;
    private int _lastHeight;
    private readonly object _resizeLock = new();

    public TerminalResizeExample()
    {
        _lastWidth = Console.WindowWidth;
        _lastHeight = Console.WindowHeight;
    }

    /// <summary>
    /// Method 1: Using Console.WindowWidth/Height polling
    /// </summary>
    public async Task RunWithPollingAsync(CancellationToken cancellationToken = default)
    {
        // Start resize detection in background
        var resizeTask = Task.Run(() => DetectResizeLoop(cancellationToken), cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Your main game loop here
                await Task.Delay(100, cancellationToken);
            }
        }
        finally
        {
            await resizeTask;
        }
    }

    private void DetectResizeLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var currentWidth = Console.WindowWidth;
                var currentHeight = Console.WindowHeight;

                lock (_resizeLock)
                {
                    if (currentWidth != _lastWidth || currentHeight != _lastHeight)
                    {
                        _lastWidth = currentWidth;
                        _lastHeight = currentHeight;
                        OnTerminalResized();
                    }
                }
            }
            catch (Exception)
            {
                // Handle console not available or other errors
            }

            Thread.Sleep(250); // Check every 250ms
        }
    }

    /// <summary>
    /// Method 2: Using AnsiConsole.Profile to detect changes
    /// </summary>
    public void CheckForResize()
    {
        var profile = AnsiConsole.Profile;
        var currentWidth = profile.Width;
        var currentHeight = profile.Height;

        if (currentWidth != _lastWidth || currentHeight != _lastHeight)
        {
            _lastWidth = currentWidth;
            _lastHeight = currentHeight;
            OnTerminalResized();
        }
    }

    private void OnTerminalResized()
    {
        // Clear and redraw everything
        AnsiConsole.Clear();
        RedrawUI();
    }

    private void RedrawUI()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(3),
                new Layout("Body"),
                new Layout("Footer").Size(3));

        layout["Header"].Update(
            new Panel($"Terminal Size: {_lastWidth}x{_lastHeight}")
                .Expand()
                .Header("Terminal Info"));

        layout["Body"].Update(
            new Panel("Content will be redrawn when terminal is resized")
                .Expand());

        layout["Footer"].Update(
            new Panel($"Last Update: {DateTime.Now:HH:mm:ss}")
                .Expand());

        AnsiConsole.Write(layout);
    }

    /// <summary>
    /// Method 3: Using Live display with auto-refresh
    /// </summary>
    public async Task RunWithLiveDisplayAsync(CancellationToken cancellationToken = default)
    {
        await AnsiConsole.Live(GenerateLiveContent())
            .AutoClear(true)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Check for resize
                    CheckForResize();

                    // Update the live content
                    ctx.UpdateTarget(GenerateLiveContent());

                    await Task.Delay(500, cancellationToken);
                }
            });
    }

    private IRenderable GenerateLiveContent()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Expand()
            .AddColumn("Property")
            .AddColumn("Value");

        table.AddRow("Terminal Width", AnsiConsole.Profile.Width.ToString());
        table.AddRow("Terminal Height", AnsiConsole.Profile.Height.ToString());
        table.AddRow("Color System", AnsiConsole.Profile.Capabilities.ColorSystem.ToString());
        table.AddRow("Unicode Support", AnsiConsole.Profile.Capabilities.Unicode.ToString());
        table.AddRow("Current Time", DateTime.Now.ToString("HH:mm:ss.fff"));

        return new Panel(table)
            .Header("Live Terminal Info")
            .Expand();
    }

    /// <summary>
    /// Method 4: For the game loop - integrating resize detection
    /// </summary>
    public class GameWithResizeDetection
    {
        private int _lastWidth;
        private int _lastHeight;
        private bool _needsRedraw = true;

        public async Task RunGameLoopAsync(CancellationToken cancellationToken)
        {
            _lastWidth = AnsiConsole.Profile.Width;
            _lastHeight = AnsiConsole.Profile.Height;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Check for terminal resize
                if (HasTerminalResized())
                {
                    _needsRedraw = true;
                }

                // Redraw if needed
                if (_needsRedraw)
                {
                    RedrawGame();
                    _needsRedraw = false;
                }

                // Handle input
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    HandleInput(key);
                }

                await Task.Delay(50, cancellationToken);
            }
        }

        private bool HasTerminalResized()
        {
            var currentWidth = AnsiConsole.Profile.Width;
            var currentHeight = AnsiConsole.Profile.Height;

            if (currentWidth != _lastWidth || currentHeight != _lastHeight)
            {
                _lastWidth = currentWidth;
                _lastHeight = currentHeight;
                return true;
            }

            return false;
        }

        private void RedrawGame()
        {
            AnsiConsole.Clear();

            // Use responsive layouts that adapt to terminal size
            var rule = new Rule($"[yellow]Game Window ({_lastWidth}x{_lastHeight})[/]")
                .RuleStyle("blue");
            AnsiConsole.Write(rule);

            // Create adaptive layout based on terminal size
            if (_lastWidth < 80)
            {
                // Compact layout for narrow terminals
                DrawCompactLayout();
            }
            else
            {
                // Full layout for wider terminals
                DrawFullLayout();
            }
        }

        private void DrawCompactLayout()
        {
            AnsiConsole.MarkupLine("[dim]Compact Mode[/]");
            // Simplified UI for small terminals
        }

        private void DrawFullLayout()
        {
            var layout = new Layout()
                .SplitColumns(
                    new Layout("Left").Ratio(1),
                    new Layout("Right").Ratio(2));

            layout["Left"].Update(new Panel("Navigation"));
            layout["Right"].Update(new Panel("Content"));

            AnsiConsole.Write(layout);
        }

        private void HandleInput(ConsoleKeyInfo key)
        {
            // Handle game input
            if (key.Key == ConsoleKey.R)
            {
                _needsRedraw = true;
            }
        }
    }
}