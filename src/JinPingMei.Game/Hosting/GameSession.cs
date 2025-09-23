using JinPingMei.Engine;

namespace JinPingMei.Game.Hosting;

public sealed class GameSession
{
    private readonly GameRuntime _runtime;

    public GameSession(GameRuntime runtime)
    {
        _runtime = runtime;
    }

    public string RenderIntro()
    {
        return _runtime.RenderIntro();
    }

    public string HandleCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "[WIP] 指令尚未實作。請輸入 'help' 或 'quit'.";
        }

        if (string.Equals(command, "help", StringComparison.OrdinalIgnoreCase))
        {
            return "Prototype 支援指令: help, quit. 故事互動即將推出。";
        }

        return $"[WIP] 已收到指令：{command}。互動劇情開發中。";
    }
}
