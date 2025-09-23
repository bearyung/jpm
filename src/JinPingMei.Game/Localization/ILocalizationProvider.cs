using System.Collections.Generic;

namespace JinPingMei.Game.Localization;

public interface ILocalizationProvider
{
    string DefaultLocale { get; }

    string GetString(string locale, string key);

    IReadOnlyDictionary<string, string> GetLocaleCatalog(string locale);
}
