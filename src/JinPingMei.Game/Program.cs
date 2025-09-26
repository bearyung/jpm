using JinPingMei.Game;
using JinPingMei.Game.Hosting;
using JinPingMei.Game.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using dotenv.net;

// Set UTF-8 encoding for console output
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Load environment variables
LoadEnvFiles();

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);

    // Check if debug logging is enabled
    var logLevel = configuration.GetValue("Logging:LogLevel:Default", "Information");
    if (Enum.TryParse<LogLevel>(logLevel, out var level))
    {
        builder.SetMinimumLevel(level);
    }

    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
});

// Setup localization
var localizationProvider = new JsonLocalizationProvider(
    Path.Combine(AppContext.BaseDirectory, "Localization"));

// Create game session factory (with a null diagnostics for console mode)
var sessionFactory = new GameSessionFactory(localizationProvider, new NullDiagnostics());

var useTerminalGui = args.Any(static arg =>
    string.Equals(arg, "--tui", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "--terminal-gui", StringComparison.OrdinalIgnoreCase));

// Setup cancellation
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

try
{
    if (useTerminalGui)
    {
        var tuiLogger = loggerFactory.CreateLogger<SpectreConsoleGame>();
        var tuiGame = new SpectreConsoleGame(tuiLogger, sessionFactory);
        await tuiGame.RunAsync(cts.Token);
    }
    else
    {
        var consoleLogger = loggerFactory.CreateLogger<ConsoleGame>();
        var game = new ConsoleGame(consoleLogger, sessionFactory);

        Console.Clear();
        Console.WriteLine("====== 金瓶梅 JinPingMei - 互動敘事實驗 ======");
        Console.WriteLine();

        await game.RunAsync(cts.Token);

        // Only wait for key press if we're in an interactive console
        if (!Console.IsInputRedirected)
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey(intercept: true);
        }
    }
}
catch (OperationCanceledException)
{
    if (!useTerminalGui)
    {
        Console.WriteLine("\nGame interrupted by user.");
    }
}
catch (Exception ex)
{
    if (!useTerminalGui)
    {
        Console.WriteLine($"\nAn error occurred: {ex.Message}");
        if (loggerFactory.CreateLogger<Program>().IsEnabled(LogLevel.Debug))
        {
            Console.WriteLine(ex.StackTrace);
        }
    }
    else
    {
        loggerFactory.CreateLogger<Program>().LogError(ex, "Unhandled exception in Terminal GUI mode");
    }
}

static void LoadEnvFiles()
{
    var candidatePaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env")
    };

    foreach (var path in candidatePaths)
    {
        if (!File.Exists(path))
        {
            continue;
        }

        DotEnv.Load(new DotEnvOptions(
            envFilePaths: new[] { path },
            overwriteExistingVars: true,
            ignoreExceptions: true));
    }
}
