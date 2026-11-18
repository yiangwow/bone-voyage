namespace BoneVoyage.Sim;

public interface ISimEvent { }

public sealed record SkeletonsSpawned(SkeletonTypeId Type, int Count) : ISimEvent;
public sealed record SkeletonsDied(SkeletonTypeId Type, int Count, int Zone, DeathCause Cause) : ISimEvent;
public sealed record WallHit(SkeletonTypeId Type, int Count, double Damage) : ISimEvent;
public sealed record KingdomFell(int KingdomIndex, int CycleTier, double BonesJackpot, double GoldJackpot) : ISimEvent;
