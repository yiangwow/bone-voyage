using System.Collections.Generic;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class ConquestStrip : Control
{
    private readonly Simulation _sim;
    private readonly List<(Label Name, Label Perk, TextureRect Flag)> _cells = new();

    public ConquestStrip(Simulation sim) => _sim = sim;

    public override void _Ready()
    {
        Position = new Vector2(400, 8);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 24);
        AddChild(row);
        foreach (var k in _sim.Balance.Kingdoms)
        {
            var cell = new VBoxContainer();
            var top = new HBoxContainer();
            var flag = new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://assets/sprites/flag.png"),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(20, 20), Visible = false,
            };
            var name = new Label();
            name.AddThemeFontSizeOverride("font_size", 16);
            top.AddChild(flag); top.AddChild(name);
            var perk = new Label { Text = PerkLabel(k.Perk) };
            perk.AddThemeFontSizeOverride("font_size", 11);
            perk.AddThemeColorOverride("font_color", new Color("c9d1bf"));
            cell.AddChild(top); cell.AddChild(perk);
            row.AddChild(cell);
            _cells.Add((name, perk, flag));
        }
    }

    private string PerkLabel(PerkBalance perk)
    {
        string pct = $"+{(perk.Mult - 1.0) * 100:0.#}%";
        return perk.Id switch
        {
            PerkId.BountyMult => $"{pct} {Tr("bounty")}",
            PerkId.SpeedMult => $"{pct} {Tr("march speed")}",
            PerkId.GoldMult => $"{pct} {Tr("gold")}",
            _ => pct,
        };
    }

    public void Refresh()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            var k = _sim.Balance.Kingdoms[i];
            bool current = i == _sim.State.KingdomIndex;
            _cells[i].Name.Text = current
                ? $"{Tr(k.Name)} {Roman(_sim.State.CycleTier)}" : Tr(k.Name);
            _cells[i].Name.AddThemeColorOverride("font_color",
                current ? new Color("ffd670") : new Color("ffffff"));
            _cells[i].Flag.Visible = i < _sim.State.PerksUnlocked;
        }
    }

    private static string Roman(int n)
    {
        if (n >= 40) return n.ToString();
        string[] tens = { "", "X", "XX", "XXX" };
        string[] ones = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
        return tens[n / 10] + ones[n % 10];
    }
}
