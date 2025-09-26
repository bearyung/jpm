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

// Create game session factory
var sessionFactory = new GameSessionFactory(localizationProvider, new NullDiagnostics());

// Setup cancellation
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

try
{
    var gameLogger = loggerFactory.CreateLogger<SpectreConsoleGame>();
    var game = new SpectreConsoleGame(gameLogger, sessionFactory);
    await game.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nGame interrupted by user.");
}
catch (Exception ex)
{
    loggerFactory.CreateLogger<Program>().LogError(ex, "Unhandled exception");
    Console.WriteLine($"\nAn error occurred: {ex.Message}");
    if (loggerFactory.CreateLogger<Program>().IsEnabled(LogLevel.Debug))
    {
        Console.WriteLine(ex.StackTrace);
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
