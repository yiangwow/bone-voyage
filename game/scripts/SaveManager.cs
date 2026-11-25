using System;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public static class SaveManager
{
    public const string SavePath = "user://save.json";

    public static SaveData? TryLoad()
    {
        if (!FileAccess.FileExists(SavePath)) return null;
        try
        {
            return SaveCodec.Parse(FileAccess.GetFileAsString(SavePath));
        }
        catch (Exception e)
        {
            GD.PushWarning($"Save unreadable, starting fresh: {e.Message}");
            return null;
        }
    }

    public static void Save(SimState state, SaveSettings settings)
    {
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (f == null)
        {
            GD.PushWarning($"Save failed: {FileAccess.GetOpenError()}");
            return;
        }
        f.StoreString(SaveCodec.ToJson(state,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), settings));
    }
}
