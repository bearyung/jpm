using System.Collections.Generic;
using JinPingMei.Game.Localization;

namespace JinPingMei.Engine.Tests;

internal sealed class TestLocalizationProvider : ILocalizationProvider
{
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _catalogs;

    public TestLocalizationProvider()
    {
        var zhTw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["commands.invalid"] = "請輸入要執行的指令，例如 /help。",
            ["commands.unknown"] = "無法識別這個指令。",
            ["commands.help.summary"] = "目前可用指令：/help、/name <名字>、/look、/say <內容>、/diagnostics、/health、/quit。",
            ["commands.help.detail"] = "所有系統指令都以 / 開頭；其他內容則會交給故事互動。",
            ["commands.name.prompt"] = "請輸入 /name 後接您的名字。",
            ["commands.name.too_long"] = "名字最長 {0} 個字元。",
            ["commands.name.confirm"] = "從現在起我們將以「{0}」稱呼您。",
            ["commands.look.description"] = "仍在搭建中的場景。",
            ["commands.say.prompt"] = "請在 /say 後輸入想說的話。",
            ["commands.say.echo"] = "{0}說：「{1}」",
            ["commands.quit.confirm"] = "期待下次再會。",
            ["session.display_name.default"] = "旅人",
            ["session.commands.hint"] = "輸入 /help 查看可用指令；其餘文字會用於故事互動。",
            ["story.empty"] = "你靜靜地沉默著。",
            ["story.placeholder"] = "{0}的話語尚未觸發劇情：「{1}」。",
            ["commands.diagnostics.header"] = "系統診斷資訊：",
            ["commands.diagnostics.uptime"] = "伺服器已連續執行 {0}。",
            ["commands.diagnostics.sessions.active"] = "目前連線數：{0}。",
            ["commands.diagnostics.sessions.total"] = "累計連線：{0}，遭拒：{1}。",
            ["commands.diagnostics.sessions.completed"] = "已結束連線：{0}。",
            ["commands.diagnostics.errors"] = "錯誤總數：{0}（會話 {1}、指令 {2}）。",
            ["commands.diagnostics.commands"] = "指令執行次數：{0}（每分鐘約 {1:F2} 次）。",
            ["commands.health.ok"] = "系統狀態：正常。",
            ["commands.health.degraded"] = "系統狀態：注意，目前累計錯誤 {0} 次。",
            ["commands.health.active"] = "當前連線數：{0}。",
            ["commands.health.started"] = "啟動時間：{0:yyyy/MM/dd HH:mm:ss}。"
        };

        _catalogs = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-TW"] = zhTw
        };
    }

    public string DefaultLocale => "zh-TW";

    public string GetString(string locale, string key)
    {
        var catalog = GetLocaleCatalog(locale);
        return catalog.TryGetValue(key, out var value)
            ? value
            : key;
    }

    public IReadOnlyDictionary<string, string> GetLocaleCatalog(string locale)
    {
        return _catalogs.TryGetValue(locale, out var value)
            ? value
            : _catalogs[DefaultLocale];
    }
}
