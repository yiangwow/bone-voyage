using System.Collections.Generic;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class Main : Node2D
{
    private Simulation _sim = null!;
    private BattlefieldView? _battlefield;   // Task 10
    private Hud _hud = null!;
    private double _acc;
    private double _saveAcc;
    private string? _shotPath;
    private int _shotCountdown = -1;

    public override void _Ready()
    {
        var json = FileAccess.GetFileAsString("res://balance.json");
        _sim = new Simulation(BalanceData.Load(json), SaveManager.TryLoad());

        // Dev tool: --kingdom=N forces a starting kingdom for screenshot/manual verification
        // of kingdoms other than the default (Meadowshire). Not exposed in the shipped UI.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--kingdom="))
            {
                if (!int.TryParse(arg["--kingdom=".Length..], out int idx)) continue;
                idx = Mathf.Clamp(idx, 0, _sim.Balance.Kingdoms.Count - 1);
                _sim.State.KingdomIndex = idx;
                _sim.State.WallHp = _sim.WallHpFor(idx, 1);
            }

        _battlefield = new BattlefieldView(_sim);
        AddChild(_battlefield);
        _hud = new Hud(_sim);
        AddChild(_hud);

        int frames = 300;
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--frames="))
                frames = int.Parse(arg["--frames=".Length..]);
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--shot="))
            { _shotPath = arg["--shot=".Length..]; _shotCountdown = frames; }
    }

    public override void _Process(double delta)
    {
        _acc += delta;
        double dt = _sim.Balance.TickSeconds;
        int safety = 0;
        while (_acc >= dt && safety++ < 120)
        {
            _acc -= dt;
            List<ISimEvent> events = _sim.Tick();
            _battlefield?.OnSimEvents(events);
        }
        _hud.Refresh();

        _saveAcc += delta;
        if (_saveAcc >= 30.0) { _saveAcc = 0; SaveManager.Save(_sim.State); }

        if (_shotCountdown > 0 && --_shotCountdown == 0)
        {
            GetViewport().GetTexture().GetImage().SavePng(_shotPath);
            SaveManager.Save(_sim.State);
            GetTree().Quit();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            SaveManager.Save(_sim.State);
            GetTree().Quit();
        }
    }
}
