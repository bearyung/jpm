using System.Collections.Generic;
using System.IO;
using System.Net;
using JinPingMei.Game.Hosting;
using JinPingMei.Game.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using dotenv.net;
using Grafana.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

Console.OutputEncoding = System.Text.Encoding.UTF8;

LoadEnvFiles();

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var hostSettings = TelnetHostSettings.FromConfiguration(configuration);
var options = configuration.GetSection("Telnet:Session").Get<TelnetGameServerOptions>() ?? new TelnetGameServerOptions();

using var loggerFactory = LoggerFactory.Create(static builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddSimpleConsole(static options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
});

using var tracerProvider = BuildTracerProvider(configuration);
using var meterProvider = BuildMeterProvider(configuration);
using var metrics = new TelnetServerMetrics();
var localizationProvider = BuildLocalizationProvider();
var sessionFactory = new GameSessionFactory(localizationProvider, metrics);

var endpoint = hostSettings.ToEndpoint();
var server = new TelnetGameServer(endpoint, options, loggerFactory.CreateLogger<TelnetGameServer>(), metrics, sessionFactory);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("====== JinPingMei AI 遊戲 Telnet Server ======");
Console.WriteLine($"Listening on {hostSettings.Host}:{hostSettings.Port}. Connect with 'telnet {hostSettings.Host} {hostSettings.Port}'.");
Console.WriteLine("Press Ctrl+C to shut down.");

try
{
    await server.StartAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Expected during shutdown.
}
finally
{
    await server.StopAsync();
}

static TracerProvider BuildTracerProvider(IConfiguration configuration)
{
    var tracerBuilder = Sdk.CreateTracerProviderBuilder()
        .AddSource(TelnetGameServer.ActivitySourceName)
        .UseGrafana(settings => ConfigureGrafanaSettings(settings, configuration, enableMetrics: false, enableTraces: true, enableLogs: false));

    if (!configuration.GetValue("Telemetry:ConsoleExporter:Disabled", false))
    {
        tracerBuilder.AddConsoleExporter();
    }

    return tracerBuilder.Build();
}

static MeterProvider BuildMeterProvider(IConfiguration configuration)
{
    var meterBuilder = Sdk.CreateMeterProviderBuilder()
        .AddMeter(TelnetServerMetrics.MeterName)
        .UseGrafana(settings => ConfigureGrafanaSettings(settings, configuration, enableMetrics: true, enableTraces: false, enableLogs: false));

    if (!configuration.GetValue("Telemetry:ConsoleExporter:Disabled", false))
    {
        meterBuilder.AddConsoleExporter();
    }

    return meterBuilder.Build();
}

static void ConfigureGrafanaSettings(GrafanaOpenTelemetrySettings settings, IConfiguration configuration, bool enableMetrics, bool enableTraces, bool enableLogs)
{
    var exporter = settings.ExporterSettings as OtlpExporter ?? new OtlpExporter();

    exporter.EnableMetrics = enableMetrics;
    exporter.EnableTraces = enableTraces;
    exporter.EnableLogs = enableLogs;

    var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? configuration["Telemetry:Otlp:Endpoint"];
    if (!string.IsNullOrWhiteSpace(endpoint) && Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
    {
        exporter.Endpoint = endpointUri;
    }

    var headers = configuration["OTEL_EXPORTER_OTLP_HEADERS"] ?? configuration["Telemetry:Otlp:Headers"];
    if (!string.IsNullOrWhiteSpace(headers))
    {
        exporter.Headers = headers;
    }

    var protocolValue = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
    if (!string.IsNullOrWhiteSpace(protocolValue))
    {
        exporter.Protocol = protocolValue.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;
    }

    settings.ExporterSettings = exporter;

    var resourceAttributes = configuration["OTEL_RESOURCE_ATTRIBUTES"];
    if (!string.IsNullOrWhiteSpace(resourceAttributes))
    {
        var resourceDictionary = settings.ResourceAttributes;
        resourceDictionary.Clear();

        var pairs = resourceAttributes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = parts[0];
            var value = parts[1];

            switch (key.ToLowerInvariant())
            {
                case "service.name":
                    settings.ServiceName = value;
                    resourceDictionary[key] = value;
                    break;
                case "service.version":
                    settings.ServiceVersion = value;
                    resourceDictionary[key] = value;
                    break;
                case "service.instance.id":
                    settings.ServiceInstanceId = value;
                    resourceDictionary[key] = value;
                    break;
                case "deployment.environment":
                    settings.DeploymentEnvironment = value;
                    resourceDictionary[key] = value;
                    break;
                default:
                    resourceDictionary[key] = value;
                    break;
            }
        }
    }

    settings.ServiceName ??= "JinPingMei.Game";
    settings.ServiceVersion ??= typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
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

        DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { path }, overwriteExistingVars: true, ignoreExceptions: true));
    }
}

static JsonLocalizationProvider BuildLocalizationProvider()
{
    var localizationPath = Path.Combine(AppContext.BaseDirectory, "Localization");
    return new JsonLocalizationProvider(localizationPath);
}
