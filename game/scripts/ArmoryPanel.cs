using System.Collections.Generic;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class ArmoryPanel : PanelContainer
{
    private readonly Simulation _sim;
    private readonly List<(UpgradeId Id, string Name, Button Btn)> _rows = new();

    public ArmoryPanel(Simulation sim) => _sim = sim;

    public override void _Ready()
    {
        Position = new Vector2(1000, 452);
        Size = new Vector2(264, 268);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        AddChild(box);
        var title = new Label { Text = Tr("Royal Armory") };
        title.AddThemeFontSizeOverride("font_size", 15);
        box.AddChild(title);
        AddRow(box, UpgradeId.Helmets, Tr("Helmets"));
        AddRow(box, UpgradeId.BatteringRams, Tr("Battering Rams"));
        AddRow(box, UpgradeId.MarchingBoots, Tr("Marching Boots"));
        AddRow(box, UpgradeId.WarBanners, Tr("War Banners"));
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
