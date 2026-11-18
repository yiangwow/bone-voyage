namespace BoneVoyage.Game;

public static class Fmt
{
    public static string Num(double v)
    {
        if (v < 1000) return ((long)v).ToString();
        string[] suffix = { "K", "M", "B", "T" };
        int i = -1;
        while (v >= 1000 && i < suffix.Length - 1) { v /= 1000; i++; }
        return $"{v:0.#}{suffix[i]}";
    }
}
