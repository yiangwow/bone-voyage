using System.Collections.Generic;
using System.Linq;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class BattlefieldView : Node2D
{
    public const float RoadStartX = 140f, WallX = 1140f, GroundY = 430f, PxPerUnit = 10f;
    private const int ActorCap = 150;

    private readonly Simulation _sim;
    private readonly List<SkeletonActor> _actors = new();
    private readonly Dictionary<SkeletonTypeId, SpriteFrames> _frames = new();
    private readonly List<Node2D> _kingdomProps = new();
    private static readonly string[] DeathAnims = { "die_wave", "die_salute" };

    public BattlefieldView(Simulation sim) => _sim = sim;

    public override void _Ready()
    {
        AddChild(new ColorRect { Color = new Color("b2d6eb"), Size = new Vector2(1280, GroundY), ZIndex = -20 });
        AddChild(new ColorRect { Color = new Color("76b262"), Position = new Vector2(0, GroundY), Size = new Vector2(1280, 720 - GroundY), ZIndex = -20 });
        AddChild(new ColorRect { Color = new Color("5e9450"), Position = new Vector2(0, GroundY + 20), Size = new Vector2(1280, 26), ZIndex = -19 }); // road strip

        foreach (SkeletonTypeId type in System.Enum.GetValues<SkeletonTypeId>())
            _frames[type] = SpriteSheetLoader.Load(
                $"res://assets/sprites/{type.ToString().ToLower()}");

        Place("res://assets/sprites/necropolis.png", new Vector2(40, GroundY - 108), 2);
        RebuildKingdom();
    }

    private void Place(string path, Vector2 pos, float scale)
    {
        AddChild(new Sprite2D
        {
            Texture = GD.Load<Texture2D>(path), Position = pos,
            Scale = new Vector2(scale, scale), Centered = false, ZIndex = -10
        });
    }

    private static string TowerSprite(string towerName) => towerName switch
    {
        "Cannon" => "tower_cannon",
        "KnightPost" => "tower_knight",
        _ => "tower_arrow",
    };

    private void RebuildKingdom()
    {
        foreach (var p in _kingdomProps) if (IsInstanceValid(p)) p.QueueFree();
        _kingdomProps.Clear();
        float zonePx = (float)_sim.Balance.ZoneLength * PxPerUnit;
        foreach (var moat in _sim.CurrentKingdom.Moats)
        {
            var tile = new Sprite2D
            {
                Texture = GD.Load<Texture2D>("res://assets/sprites/moat.png"),
                Position = new Vector2(RoadStartX + moat.Zone * zonePx, GroundY + 18),
                Scale = new Vector2(zonePx / 24f, 1.2f),
                Centered = false, ZIndex = -15,
            };
            _kingdomProps.Add(tile); AddChild(tile);
        }
        foreach (var tower in _sim.CurrentKingdom.Towers)
        {
            float x = RoadStartX + (tower.Zone + 0.5f) * zonePx;
            var s = new Sprite2D
            {
                Texture = GD.Load<Texture2D>($"res://assets/sprites/{TowerSprite(tower.Name)}.png"),
                Position = new Vector2(x - 24, GroundY - 76),
                Scale = new Vector2(2, 2), Centered = false, ZIndex = -10,
            };
            _kingdomProps.Add(s); AddChild(s);
        }
        var wall = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://assets/sprites/wall.png"),
            Position = new Vector2(WallX, GroundY - 300),
            Scale = new Vector2(2, 2), Centered = false, ZIndex = -10,
        };
        _kingdomProps.Add(wall); AddChild(wall);
    }

    public void OnSimEvents(List<ISimEvent> events)
    {
        foreach (var e in events)
        {
            switch (e)
            {
                case SkeletonsSpawned s: SpawnActors(s); break;
                case SkeletonsDied d when d.Cause == DeathCause.Tower: KillActors(d); break;
                case WallHit w: WhackActors(w); break;
                case KingdomFell: RebuildKingdom(); break;
                // WallCrumble deaths are covered by the whack animation
            }
        }
    }

    private void SpawnActors(SkeletonsSpawned e)
    {
        int alive = _actors.Count(a => IsInstanceValid(a));
        _actors.RemoveAll(a => !IsInstanceValid(a));
        for (int i = 0; i < e.Count && alive + i < ActorCap; i++)
        {
            float speedPx = (float)_sim.EffSpeed(e.Type) * PxPerUnit;
            var actor = new SkeletonActor(e.Type, _frames[e.Type], speedPx, WallX - 20f)
            {
                Position = new Vector2(RoadStartX, GroundY + (float)GD.RandRange(-6, 18)),
            };
            actor.ZIndex = (int)actor.Position.Y; // painter's order by depth
            _actors.Add(actor);
            AddChild(actor);
        }
    }

    private void KillActors(SkeletonsDied e)
    {
        float centerX = RoadStartX + (e.Zone + 0.5f) * (float)_sim.Balance.ZoneLength * PxPerUnit;
        var victims = _actors
            .Where(a => IsInstanceValid(a) && a.Walking && a.Type == e.Type)
            .OrderBy(a => Mathf.Abs(a.Position.X - centerX))
            .Take(e.Count);
        foreach (var v in victims)
            v.PlayDeath(DeathAnims[GD.RandRange(0, DeathAnims.Length - 1)]);
    }

    private void WhackActors(WallHit e)
    {
        var victims = _actors
            .Where(a => IsInstanceValid(a) && a.Walking && a.Type == e.Type)
            .OrderByDescending(a => a.Position.X)
            .Take(e.Count);
        foreach (var v in victims)
            v.PlayDeath("whack");
    }
}
