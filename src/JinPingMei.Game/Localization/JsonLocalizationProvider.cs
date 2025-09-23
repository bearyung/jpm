using System.Collections.Concurrent;
using System.Text.Json;

namespace JinPingMei.Game.Localization;

public sealed class JsonLocalizationProvider : ILocalizationProvider
{
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _catalogs = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _basePath;

    public JsonLocalizationProvider(string basePath, string defaultLocale = "zh-TW")
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path must be provided", nameof(basePath));
        }

        if (!Directory.Exists(basePath))
        {
            throw new DirectoryNotFoundException($"Localization directory not found: {basePath}");
        }

        _basePath = basePath;
        DefaultLocale = defaultLocale;
    }

    public string DefaultLocale { get; }

    public string GetString(string locale, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var localeCatalog = GetLocaleCatalog(locale);
        if (localeCatalog.TryGetValue(key, out var value))
        {
            return value;
        }

        if (!string.Equals(locale, DefaultLocale, StringComparison.OrdinalIgnoreCase))
        {
            var fallbackCatalog = GetLocaleCatalog(DefaultLocale);
            if (fallbackCatalog.TryGetValue(key, out var fallbackValue))
            {
                return fallbackValue;
            }
        }

        return key;
    }

    public IReadOnlyDictionary<string, string> GetLocaleCatalog(string locale)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale;

        return _catalogs.GetOrAdd(normalizedLocale, LoadLocale);
    }

    private IReadOnlyDictionary<string, string> LoadLocale(string locale)
    {
        var localePath = Path.Combine(_basePath, locale);
        if (!Directory.Exists(localePath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(localePath, "*.json", SearchOption.AllDirectories))
        {
            using var stream = File.OpenRead(file);
            var document = JsonDocument.Parse(stream);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    _ => property.Value.ToString()
                };

                result[property.Name] = value;
            }
        }

        return result;
    }
}
