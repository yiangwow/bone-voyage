using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

/// <summary>"While you were away" shift report: shown once at startup (or
/// after an OS sleep) when the sim replayed >= 60 s of offline time.</summary>
public partial class AwayReport : CanvasLayer
{
    private readonly OfflineReport _report;

    public AwayReport(OfflineReport report)
    {
        _report = report;
        Layer = 10;
    }

    public override void _Ready()
    {
        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.45f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(dim);

        // CenterContainer re-centers its child every time the child's
        // minimum size changes, so the panel lands in the middle of the
        // screen once the VBoxContainer's content settles — no need to
        // chase container layout timing with a deferred anchor call.
        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.09f, 0.11f, 0.96f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
        };
        panelStyle.SetContentMarginAll(16);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        var box = new VBoxContainer { CustomMinimumSize = new Vector2(430, 0) };
        box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);

        AddLine(box, Tr("While you were away…"), 24);
        AddLine(box, string.Format(Tr("The night shift ran for {0}."),
            FormatSpan(_report.SimulatedSeconds)), 14);
        AddLine(box, string.Format(
            Tr("Your employees recycled {0} of themselves. The union is proud."),
            Fmt.Num(_report.Recycled)), 14);
        AddLine(box, string.Format(Tr("Shift earnings: {0} bones, {1} gold."),
            Fmt.Num(_report.BonesEarned), Fmt.Num(_report.GoldEarned)), 14);
        if (_report.KingdomsFell > 0)
            AddLine(box, string.Format(Tr("Kingdoms liberated while you slept: {0}."),
                _report.KingdomsFell), 14);
        if (_report.Capped)
            AddLine(box, Tr("(The union caps overtime at 24 hours. Rules are rules.)"), 12);

        var btn = new Button { Text = Tr("Back to work!"), CustomMinimumSize = new Vector2(0, 36) };
        btn.Pressed += QueueFree;
        box.AddChild(btn);
    }

    private void AddLine(VBoxContainer box, string text, int size)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        l.AddThemeFontSizeOverride("font_size", size);
        box.AddChild(l);
    }

    private static string FormatSpan(double seconds)
    {
        int h = (int)(seconds / 3600), m = (int)(seconds % 3600 / 60);
        return h > 0 ? $"{h}h {m:00}m" : $"{m}m";
    }
}
