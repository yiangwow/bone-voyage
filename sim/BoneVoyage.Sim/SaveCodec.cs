using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoneVoyage.Sim;

/// <summary>Player preferences carried inside the save file. Inert data —
/// the sim never reads it; the game layer owns its meaning.</summary>
public sealed record SaveSettings(double MusicVolume, double SfxVolume)
{
    public static readonly SaveSettings Default = new(0.8, 0.8);
    public SaveSettings Clamped() =>
        new(Math.Clamp(MusicVolume, 0.0, 1.0), Math.Clamp(SfxVolume, 0.0, 1.0));
}

public sealed record SaveData(int Version, long TimestampUtcSeconds, double Bones, double Gold,
    int KingdomIndex, int CycleTier, int PerksUnlocked, int TotalFalls, double WallHp,
    Dictionary<UpgradeId, int> UpgradeLevels, int[][] Counts, SaveSettings? Settings = null);

public static class SaveCodec
{
    public const int CurrentVersion = 2;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ToJson(SimState state, long timestampUtcSeconds, SaveSettings settings)
    {
        int types = state.Counts.GetLength(0), zones = state.Counts.GetLength(1);
        var counts = new int[types][];
        for (int t = 0; t < types; t++)
        {
            counts[t] = new int[zones];
            for (int z = 0; z < zones; z++) counts[t][z] = state.Counts[t, z];
        }
        var data = new SaveData(CurrentVersion, timestampUtcSeconds, state.Bones, state.Gold,
            state.KingdomIndex, state.CycleTier, state.PerksUnlocked, state.TotalFalls,
            state.WallHp, new(state.UpgradeLevels), counts, settings);
        return JsonSerializer.Serialize(data, Options);
    }

    public static SaveData Parse(string json)
    {
        var data = JsonSerializer.Deserialize<SaveData>(json, Options)
            ?? throw new ArgumentException("save deserialized to null");
        if (data.Version > CurrentVersion)
            throw new ArgumentException($"save version {data.Version} is newer than supported {CurrentVersion}");
        if (data.UpgradeLevels == null || data.Counts == null)
            throw new ArgumentException("save is missing upgradeLevels or counts");
        return data;
    }
}
