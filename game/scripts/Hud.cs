using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class Hud : CanvasLayer
{
    private readonly Simulation _sim;
    private Label _bones = null!, _gold = null!, _employees = null!;
    private ProgressBar _wallBar = null!;
    private ShopPanel _shop = null!;
    private ArmoryPanel _armory = null!;
    private ConquestStrip _strip = null!;

    public Hud(Simulation sim) => _sim = sim;

    public override void _Ready()
    {
        _bones = MakeLabel(new Vector2(16, 12), 28);
        _gold = MakeLabel(new Vector2(16, 44), 20);
        _gold.AddThemeColorOverride("font_color", new Color("ffd670"));
        _employees = MakeLabel(new Vector2(16, 100), 14);

        _wallBar = new ProgressBar
        {
            Position = new Vector2(980, 16), Size = new Vector2(284, 22),
            ShowPercentage = false, MaxValue = 1.0
        };
        AddChild(_wallBar);

        _shop = new ShopPanel(_sim);
        AddChild(_shop);

        _armory = new ArmoryPanel(_sim);
        AddChild(_armory);

        _strip = new ConquestStrip(_sim);
        AddChild(_strip);
    }

    private Label MakeLabel(Vector2 pos, int size)
    {
        var l = new Label { Position = pos };
        l.AddThemeFontSizeOverride("font_size", size);
        AddChild(l);
        return l;
    }

    public void Refresh()
    {
        _bones.Text = $"{Tr("Bones")}: {Fmt.Num(_sim.State.Bones)}";
        _gold.Text = $"{Tr("Gold")}: {Fmt.Num(_sim.State.Gold)}";
        int alive = 0;
        for (int t = 0; t < System.Enum.GetValues<SkeletonTypeId>().Length; t++)
            for (int z = 0; z < _sim.Balance.ZoneCount; z++)
                alive += _sim.State.Counts[t, z];
        _employees.Text = $"{Tr("Employees on shift")}: {Fmt.Num(alive)}";
        _wallBar.Value = _sim.State.WallHp / _sim.WallHpFor(_sim.State.KingdomIndex, _sim.State.CycleTier);
        _shop.Refresh();
        _armory.Refresh();
        _strip.Refresh();
    }
}
