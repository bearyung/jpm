namespace JinPingMei.Game.Hosting.Text;

internal static class EraseSequences
{
    public static string ForWidth(int width)
    {
        if (width <= 0)
        {
            width = 1;
        }

        return new string('\b', width) + new string(' ', width) + new string('\b', width);
    }
}
