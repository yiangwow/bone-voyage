using System.Globalization;
using System.Text;
using BoneVoyage.Sim;

namespace BoneVoyage.BalanceHarness;

/// <summary>Headless greedy-buyer progression harness. Replays the
/// deterministic sim for --hours of game time; every sim-second it repeatedly
/// buys the cheapest affordable upgrade (a maximally impatient shopkeeper).
/// Prints kingdom-fall timings and optionally writes a progression CSV so
/// pacing is tuned from data, not vibes.</summary>
public static class Program
{
    public static int Main(string[] args)
    {
        string balancePath = "game/balance.json";
        double hours = 20.0, csvEverySeconds = 10.0;
        string? csvPath = null;
        for (int i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--hours": hours = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--csv": csvPath = args[++i]; break;
                case "--interval": csvEverySeconds = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                default: balancePath = args[i]; break;
            }
        if (!File.Exists(balancePath))
        {
            Console.Error.WriteLine($"balance file not found: {balancePath}");
            return 1;
        }

        var balance = BalanceData.Load(File.ReadAllText(balancePath));
        var sim = new Simulation(balance);
        var upgrades = balance.Upgrades.Keys.ToArray();

        var csv = new StringBuilder("t_seconds,bones,gold,total_falls,cycle_tier,kingdom_index,employees\n");
        var falls = new List<(double T, KingdomFell E)>();

        long ticksPerSecond = (long)Math.Round(1.0 / balance.TickSeconds);
        long csvEveryTicks = Math.Max(1, (long)Math.Round(csvEverySeconds / balance.TickSeconds));
        long totalTicks = (long)(hours * 3600.0 / balance.TickSeconds);

        for (long tick = 1; tick <= totalTicks; tick++)
        {
            foreach (var e in sim.Tick())
                if (e is KingdomFell f)
                {
                    double t = tick * balance.TickSeconds;
                    falls.Add((t, f));
                    Console.WriteLine($"fall #{falls.Count} t={t:0}s ({t / 60.0:0.0} min)  " +
                        $"{balance.Kingdoms[f.KingdomIndex].Name} tier {f.CycleTier}");
                }

            if (tick % ticksPerSecond == 0) BuyGreedily(sim, upgrades);
            if (tick % csvEveryTicks == 0) AppendRow(csv, sim, tick * balance.TickSeconds);
        }

        Console.WriteLine($"summary: {falls.Count} falls in {hours}h; " +
            $"final bones={sim.State.Bones:0}, gold={sim.State.Gold:0}, tier={sim.State.CycleTier}");
        if (csvPath != null)
        {
            File.WriteAllText(csvPath, csv.ToString());
            Console.WriteLine($"csv: {csvPath}");
        }
        return 0;
    }

    private static void BuyGreedily(Simulation sim, UpgradeId[] upgrades)
    {
        while (true)
        {
            UpgradeId? best = null;
            double bestCost = double.MaxValue;
            foreach (var id in upgrades)
            {
                double cost = sim.GetUpgradeCost(id);
                double wallet = sim.Balance.Upgrades[id].Currency == Currency.Gold
                    ? sim.State.Gold : sim.State.Bones;
                if (cost <= wallet && cost < bestCost) { best = id; bestCost = cost; }
            }
            if (best == null) return;
            sim.TryPurchase(best.Value);
        }
    }

    private static void AppendRow(StringBuilder csv, Simulation sim, double t)
    {
        int alive = 0;
        for (int ty = 0; ty < sim.State.Counts.GetLength(0); ty++)
            for (int z = 0; z < sim.State.Counts.GetLength(1); z++)
                alive += sim.State.Counts[ty, z];
        csv.Append(string.Create(CultureInfo.InvariantCulture,
            $"{t:0},{sim.State.Bones:0.##},{sim.State.Gold:0.##},{sim.State.TotalFalls}," +
            $"{sim.State.CycleTier},{sim.State.KingdomIndex},{alive}\n"));
    }
}
