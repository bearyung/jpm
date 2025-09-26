using System;
using System.Threading;
using System.Threading.Tasks;
using JinPingMei.Game.Hosting;
using JinPingMei.Game.Hosting.Commands;
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
                continue;
            }

            // Display response
            foreach (var responseLine in commandResult.Lines)
            {
                await _terminal.WriteLineAsync(responseLine, cancellationToken);
            }

            // Check if user wants to quit
            if (commandResult.ShouldDisconnect)
            {
                _logger.LogInformation("User requested to quit");
                break;
            }
        }
    }
}

/// <summary>
/// Simple terminal wrapper for console I/O
/// </summary>
internal sealed class ConsoleTerminal
{
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

    public ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<string?>(cancellationToken);
        }

        try
        {
            var line = Console.ReadLine();
            return ValueTask.FromResult(line);
        }
        catch (Exception)
        {
            // End of input or error
            return ValueTask.FromResult<string?>(null);
        }
    }
}