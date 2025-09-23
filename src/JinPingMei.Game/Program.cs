using JinPingMei.Engine;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var runtime = GameRuntime.CreateDefault();

Console.WriteLine("====== JinPingMei AI 遊戲 Prototype ======");
Console.WriteLine(runtime.RenderIntro());
Console.WriteLine();
Console.WriteLine("Prototype runtime wiring complete. Interactive loop will arrive in upcoming iterations.");
