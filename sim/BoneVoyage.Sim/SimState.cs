using System.Collections.Generic;

namespace BoneVoyage.Sim;

public sealed class SimState
{
    public double Bones;
    public double Gold;
    public int KingdomIndex;
    public int CycleTier = 1;
    public int PerksUnlocked;
    public int TotalFalls;
    public double WallHp;
    public int[,] Counts;        // [skeleton type, zone]
    public double[] SpawnAcc;    // [skeleton type]
    public double[,] MoveAcc;    // [skeleton type, zone]
    public double[] DamagePool;  // [zone]
    public Dictionary<UpgradeId, int> UpgradeLevels = new();

    public SimState(int typeCount, int zoneCount)
    {
        Counts = new int[typeCount, zoneCount];
        SpawnAcc = new double[typeCount];
        MoveAcc = new double[typeCount, zoneCount];
        DamagePool = new double[zoneCount];
    }
}
