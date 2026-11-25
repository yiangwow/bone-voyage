using System.Collections.Generic;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public static class ShopRow
{
    private static readonly HashSet<UpgradeId> Percent = new()
    {
        UpgradeId.Recovery, UpgradeId.Helmets, UpgradeId.BatteringRams,
        UpgradeId.MarchingBoots, UpgradeId.WarBanners,
    };

    public static string EffectLabel(Simulation sim, UpgradeId id)
    {
        var u = sim.Balance.Upgrades[id];
        return Percent.Contains(id) ? $"+{u.PerLevel * 100:0.#}%" : $"+{u.PerLevel:0.##}/s";
    }

    public static void Refresh(Simulation sim, UpgradeId id, string name, Button btn)
    {
        int lvl = sim.State.UpgradeLevels.GetValueOrDefault(id);
        double cost = sim.GetUpgradeCost(id);
        bool gold = sim.Balance.Upgrades[id].Currency == Currency.Gold;
        double wallet = gold ? sim.State.Gold : sim.State.Bones;
        string unit = Godot.TranslationServer.Translate(gold ? "gold" : "bones");
        btn.Text = $"{name} Lv{lvl} ({EffectLabel(sim, id)})\n{Fmt.Num(cost)} {unit}";
        btn.Disabled = wallet < cost;
    }
}

public partial class ShopPanel : PanelContainer
{
    private readonly Simulation _sim;
    private readonly List<(UpgradeId Id, string Name, Button Btn)> _rows = new();

    public ShopPanel(Simulation sim) => _sim = sim;

    public override void _Ready()
    {
        AnchorTop = 1; AnchorBottom = 1;
        OffsetLeft = 16; OffsetRight = 280;
        OffsetTop = -284; OffsetBottom = -16;
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        AddChild(box);
        var title = new Label { Text = Tr("Necropolis HR") };
        title.AddThemeFontSizeOverride("font_size", 15);
        box.AddChild(title);
        AddRow(box, UpgradeId.MarcherRate, Tr("Hire Marchers"));
        AddRow(box, UpgradeId.ShieldbearerRate, Tr("Hire Shieldbearers"));
        AddRow(box, UpgradeId.SprinterRate, Tr("Hire Sprinters"));
        AddRow(box, UpgradeId.BomberRate, Tr("Hire Bombers"));
        AddRow(box, UpgradeId.BannerRate, Tr("Hire Bannerbones"));
        AddRow(box, UpgradeId.Recovery, Tr("Death Benefits"));
    }

    private void AddRow(VBoxContainer box, UpgradeId id, string name)
    {
        var btn = new Button { CustomMinimumSize = new Vector2(248, 32) };
        btn.AddThemeFontSizeOverride("font_size", 11);
        btn.Pressed += () => _sim.TryPurchase(id);
        box.AddChild(btn);
        _rows.Add((id, name, btn));
    }

    public void Refresh()
    {
        foreach (var (id, name, btn) in _rows)
            ShopRow.Refresh(_sim, id, name, btn);
    }
}
