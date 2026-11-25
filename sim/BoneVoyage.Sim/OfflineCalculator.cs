using System;

namespace BoneVoyage.Sim;

/// <summary>Result of replaying the deterministic sim over time that passed
/// while the game was closed (or the OS slept).</summary>
public sealed record OfflineReport(
    double ElapsedSeconds,    // wall-clock seconds since the save timestamp, floored at 0
    double SimulatedSeconds,  // seconds actually replayed after the cap
    bool Capped,              // true when ElapsedSeconds exceeded CapSeconds
    long Recycled,            // skeletons that died (were recycled) during the replay
    double BonesEarned,
    double GoldEarned,
    int KingdomsFell);

public static class OfflineCalculator
{
    public const double CapSeconds = 24.0 * 3600.0;

    /// <summary>Advances <paramref name="sim"/> in place by running the same
    /// deterministic ticks the live game would have run, aggregating what
    /// happened. Events are consumed here and never reach the view: offline
    /// progress is a report, not a replay.</summary>
    public static OfflineReport Run(Simulation sim, double elapsedSeconds)
    {
        double elapsed = Math.Max(0.0, elapsedSeconds);   // clock-skew guard
        bool capped = elapsed > CapSeconds;
        double simulated = capped ? CapSeconds : elapsed;
        long ticks = (long)(simulated / sim.Balance.TickSeconds);

        double bones0 = sim.State.Bones, gold0 = sim.State.Gold;
        long recycled = 0;
        int fell = 0;
        for (long i = 0; i < ticks; i++)
            foreach (var e in sim.Tick())
                switch (e)
                {
                    case SkeletonsDied d: recycled += d.Count; break;
                    case KingdomFell: fell++; break;
                }

        return new OfflineReport(elapsed, ticks * sim.Balance.TickSeconds, capped,
            recycled, sim.State.Bones - bones0, sim.State.Gold - gold0, fell);
    }
}
