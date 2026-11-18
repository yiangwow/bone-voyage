namespace BoneVoyage.Sim;

public enum SkeletonTypeId { Marcher = 0, Shieldbearer = 1, Sprinter = 2, Bomber = 3, Bannerbones = 4 }

public enum DeathCause { Tower, WallCrumble }

public enum UpgradeId { MarcherRate, ShieldbearerRate, Recovery, SprinterRate, BomberRate, BannerRate,
                        Helmets, BatteringRams, MarchingBoots, WarBanners }

public enum Currency { Bones, Gold }

public enum PerkId { BountyMult, SpeedMult, GoldMult }
