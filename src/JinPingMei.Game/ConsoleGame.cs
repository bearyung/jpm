using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JinPingMei.Game.Hosting;
using JinPingMei.Game.Hosting.Commands;
using JinPingMei.Game.Hosting.Text;
using JinPingMei.Game.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JinPingMei.Game;

public sealed class ConsoleGame
{
    private readonly ILogger<ConsoleGame> _logger;
    private readonly GameSession _gameSession;
    private readonly ConsoleTerminal _terminal;

    public ConsoleGame(
        ILogger<ConsoleGame>? logger = null,
        IGameSessionFactory? sessionFactory = null)
    {
        _logger = logger ?? NullLogger<ConsoleGame>.Instance;
        var factory = sessionFactory ?? new GameSessionFactory(
            new JsonLocalizationProvider(
                Path.Combine(AppContext.BaseDirectory, "Localization")),
            new NullDiagnostics());
        _gameSession = factory.Create();
        _terminal = new ConsoleTerminal();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting console game session");

        try
        {
            // Display welcome message
            await DisplayWelcomeAsync(cancellationToken);

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
            await _terminal.WriteLineAsync("An error occurred. The game will now exit.", cancellationToken);
        }
        finally
        {
            await _terminal.WriteLineAsync("Goodbye!", cancellationToken);
            _logger.LogInformation("Console game session ended");
        }
    }

    private async Task DisplayWelcomeAsync(CancellationToken cancellationToken)
    {
        await _terminal.WriteLineAsync("歡迎來到《金瓶梅》互動敘事實驗。", cancellationToken);
        await _terminal.WriteLineAsync(" 輸入 '/help' 查看指令清單，輸入 '/quit' 離線。", cancellationToken);
        await _terminal.WriteLineAsync(string.Empty, cancellationToken);
        await _terminal.WriteLineAsync(_gameSession.RenderIntro(), cancellationToken);
        await _terminal.WriteLineAsync(_gameSession.GetCommandHint(), cancellationToken);
    }

    private async Task RunGameLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Only use advanced terminal features if not redirected
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                // Clear screen and redraw everything
                await _terminal.ClearScreenAsync(cancellationToken);

                // Display footer
                await DisplayFooterAsync(cancellationToken);

                // Position cursor at input line (leaving space for footer)
                await _terminal.PositionCursorForInputAsync(cancellationToken);
            }

            // Display prompt
            await _terminal.WriteAsync("> ", cancellationToken);

            // Read user input
            var input = await _terminal.ReadLineAsync(cancellationToken);

            if (input is null)
            {
                // End of input stream
                break;
            }

            // Process the command
            var trimmedInput = input.Trim();

            if (string.IsNullOrEmpty(trimmedInput))
            {
                continue;
            }

            _logger.LogDebug("Processing input: {Input}", trimmedInput);

            CommandResult commandResult;
            try
            {
                commandResult = _gameSession.HandleInput(trimmedInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command: {Command}", trimmedInput);
                await _terminal.WriteLineAsync("Error processing command. Please try again.", cancellationToken);
                await _terminal.WriteLineAsync("Press Enter to continue...", cancellationToken);
                await _terminal.ReadLineAsync(cancellationToken);
                continue;
            }

            // Display response with footer intact
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                await DisplayResponseWithFooterAsync(commandResult, cancellationToken);
            }
            else
            {
                // Simple display for piped/redirected output
                foreach (var responseLine in commandResult.Lines)
                {
                    await _terminal.WriteLineAsync(responseLine, cancellationToken);
                }
            }

            // Check if user wants to quit
            if (commandResult.ShouldDisconnect)
            {
                _logger.LogInformation("User requested to quit");
                break;
            }
        }
    }

    private async Task DisplayResponseWithFooterAsync(CommandResult result, CancellationToken cancellationToken)
    {
        // Clear the main content area but keep footer
        await _terminal.ClearMainAreaAsync(cancellationToken);

        // Display response lines
        foreach (var responseLine in result.Lines)
        {
            await _terminal.WriteLineAsync(responseLine, cancellationToken);
        }

        await _terminal.WriteLineAsync("", cancellationToken);
        await _terminal.WriteLineAsync("Press Enter to continue...", cancellationToken);

        // Wait for user to press Enter
        await _terminal.ReadLineAsync(cancellationToken);
    }

    private async Task DisplayFooterAsync(CancellationToken cancellationToken)
    {
        // Only display footer for interactive terminals
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            return;

        var playerName = _gameSession.State.HasPlayerName
            ? _gameSession.State.PlayerName
            : "旅人";

        var locationName = _gameSession.GetCurrentLocationDisplayName();

        var shortcuts = "Ctrl+C:退出 | /help:指令 | /quit:離線";

        // Position cursor at bottom of screen
        await _terminal.PositionCursorAtBottomAsync(cancellationToken);

        // Draw separator line
        var width = Console.WindowWidth;
        var separator = new string('─', Math.Max(1, width));
        await _terminal.WriteLineAsync(separator, cancellationToken);

        // Format footer line: Player | Location | Shortcuts
        var footerText = $"玩家: {playerName} | 位置: {locationName} | {shortcuts}";

        // Truncate if too long for terminal width
        if (footerText.Length > width)
        {
            footerText = footerText.Substring(0, Math.Max(1, width - 3)) + "...";
        }

        await _terminal.WriteLineAsync(footerText, cancellationToken);
    }

}

/// <summary>
/// Enhanced terminal wrapper for console I/O with proper multibyte character support
/// </summary>
internal sealed class ConsoleTerminal
{
    private readonly GraphemeBuffer _buffer = new();
    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();

    public ValueTask WriteAsync(string text, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        Console.Write(text);
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteLineAsync(string text, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        Console.WriteLine(text);
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearScreenAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearMainAreaAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        if (!Console.IsOutputRedirected)
        {
            // Move cursor to top and clear downward (leaving footer at bottom)
            Console.SetCursorPosition(0, 0);
            var height = Console.WindowHeight - 3; // Leave space for separator and footer
            for (int i = 0; i < height; i++)
            {
                Console.WriteLine(new string(' ', Console.WindowWidth));
            }
            Console.SetCursorPosition(0, 0);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask PositionCursorAtBottomAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        if (!Console.IsOutputRedirected)
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 2);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask PositionCursorForInputAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        if (!Console.IsOutputRedirected)
        {
            // Position cursor for input (above footer)
            Console.SetCursorPosition(0, Console.WindowHeight - 4);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<string?>(cancellationToken);
        }

        // If input is redirected (piped), use standard ReadLine
        if (Console.IsInputRedirected)
        {
            try
            {
                var line = Console.ReadLine();
                return ValueTask.FromResult(line);
            }
            catch (Exception)
            {
                return ValueTask.FromResult<string?>(null);
            }
        }

        // For interactive console, use custom input handling with proper multibyte support
        _buffer.MoveCursorToStart();
        _buffer.TryDrain(out _); // Clear buffer

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<string?>(cancellationToken);
            }

            var key = Console.ReadKey(intercept: true);

            // Handle special keys
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                _buffer.TryDrain(out var result);
                return ValueTask.FromResult<string?>(result);
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                HandleBackspace();
                continue;
            }

            if (key.Key == ConsoleKey.Delete)
            {
                HandleDelete();
                continue;
            }

            if (key.Key == ConsoleKey.LeftArrow)
            {
                HandleLeftArrow();
                continue;
            }

            if (key.Key == ConsoleKey.RightArrow)
            {
                HandleRightArrow();
                continue;
            }

            if (key.Key == ConsoleKey.Home)
            {
                HandleHome();
                continue;
            }

            if (key.Key == ConsoleKey.End)
            {
                HandleEnd();
                continue;
            }

            // Handle regular character input
            if (key.KeyChar != '\0' && !char.IsControl(key.KeyChar))
            {
                // Convert character to Rune for proper Unicode handling
                if (Rune.TryCreate(key.KeyChar, out var rune))
                {
                    AppendRune(rune);
                }
                else if (char.IsHighSurrogate(key.KeyChar))
                {
                    // Handle surrogate pairs for characters outside BMP
                    var highSurrogate = key.KeyChar;
                    var nextKey = Console.ReadKey(intercept: true);
                    if (char.IsLowSurrogate(nextKey.KeyChar))
                    {
                        if (Rune.TryCreate(highSurrogate, nextKey.KeyChar, out rune))
                        {
                            AppendRune(rune);
                        }
                    }
                }
            }
        }
    }

    private void AppendRune(Rune rune)
    {
        var textAfterCursor = _buffer.GetTextAfterCursor();
        var widthAfterCursor = _buffer.GetDisplayWidthAfterCursor();

        _buffer.Append(rune);

        // Echo the character
        var ch = rune.ToString();
        Console.Write(ch);

        // If inserting in middle, rewrite text after cursor
        if (!string.IsNullOrEmpty(textAfterCursor))
        {
            Console.Write(textAfterCursor);
            // Move cursor back to correct position
            for (int i = 0; i < widthAfterCursor; i++)
            {
                Console.Write('\b');
            }
        }
    }

    private void HandleBackspace()
    {
        // Check if we're deleting in the middle
        var isMiddleDeletion = _buffer.CursorPosition < _buffer.Length;

        // Get text after cursor before deletion
        string? textAfterCursor = null;
        int widthAfterCursor = 0;
        if (isMiddleDeletion)
        {
            textAfterCursor = _buffer.GetTextAfterCursor();
            widthAfterCursor = _buffer.GetDisplayWidthAfterCursor();
        }

        if (!_buffer.TryBackspace(out var width))
        {
            return;
        }

        if (width <= 0)
        {
            width = 1;
        }

        if (isMiddleDeletion && !string.IsNullOrEmpty(textAfterCursor))
        {
            // Move cursor back by the width of deleted character
            for (int i = 0; i < width; i++)
            {
                Console.Write('\b');
            }
            // Write the text that was after the cursor
            Console.Write(textAfterCursor);
            // Write spaces to clear any leftover characters
            for (int i = 0; i < width; i++)
            {
                Console.Write(' ');
            }
            // Move cursor back to the correct position
            for (int i = 0; i < widthAfterCursor + width; i++)
            {
                Console.Write('\b');
            }
        }
        else
        {
            // Standard backspace at end of line
            var sequence = EraseSequences.ForWidth(width);
            Console.Write(sequence);
        }
    }

    private void HandleDelete()
    {
        var textAfterCursor = _buffer.GetTextAfterCursor();
        var widthAfterCursor = _buffer.GetDisplayWidthAfterCursor();

        if (!_buffer.TryDelete(out var width))
        {
            return;
        }

        // Rewrite text after cursor
        if (!string.IsNullOrEmpty(textAfterCursor))
        {
            // Skip the first grapheme cluster (the one we deleted)
            var remainingText = textAfterCursor;
            var remainingStart = 0;
            var enumerator = StringInfo.GetTextElementEnumerator(textAfterCursor);
            if (enumerator.MoveNext())
            {
                remainingStart = enumerator.ElementIndex + ((string)enumerator.Current).Length;
                if (remainingStart < textAfterCursor.Length)
                {
                    remainingText = textAfterCursor.Substring(remainingStart);
                }
                else
                {
                    remainingText = string.Empty;
                }
            }

            Console.Write(remainingText);
            // Clear leftover characters
            for (int i = 0; i < width; i++)
            {
                Console.Write(' ');
            }
            // Move cursor back
            for (int i = 0; i < widthAfterCursor; i++)
            {
                Console.Write('\b');
            }
        }
    }

    private void HandleLeftArrow()
    {
        if (_buffer.MoveCursorLeft(out var width))
        {
            for (int i = 0; i < width; i++)
            {
                Console.Write('\b');
            }
        }
    }

    private void HandleRightArrow()
    {
        var textAfterCursor = _buffer.GetTextAfterCursor();
        if (_buffer.MoveCursorRight(out var width) && !string.IsNullOrEmpty(textAfterCursor))
        {
            // Move cursor forward by reading the actual character(s)
            var enumerator = StringInfo.GetTextElementEnumerator(textAfterCursor);
            if (enumerator.MoveNext())
            {
                Console.Write((string)enumerator.Current);
            }
        }
    }

    private void HandleHome()
    {
        var totalWidth = 0;
        while (_buffer.MoveCursorLeft(out var width))
        {
            totalWidth += width;
        }

        if (totalWidth > 0)
        {
            for (int i = 0; i < totalWidth; i++)
            {
                Console.Write('\b');
            }
        }
    }

    private void HandleEnd()
    {
        var text = _buffer.GetTextAfterCursor();
        if (!string.IsNullOrEmpty(text))
        {
            Console.Write(text);
            while (_buffer.MoveCursorRight(out _))
            {
                // Just moving the buffer cursor, console cursor already at end
            }
        }
    }
}