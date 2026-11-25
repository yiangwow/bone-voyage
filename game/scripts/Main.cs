using System.Collections.Generic;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class Main : Node2D
{
    private Simulation _sim = null!;
    private BattlefieldView? _battlefield;   // Task 10
    private Hud _hud = null!;
    private SaveSettings _settings = SaveSettings.Default;
    private double _acc;
    private double _saveAcc;
    private string? _shotPath;
    private int _shotCountdown = -1;
    private long _lastUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private const double MinOfflineSeconds = 60.0;

    public override void _Ready()
    {
        var json = FileAccess.GetFileAsString("res://balance.json");
        SaveData? save = SaveManager.TryLoad();
        _settings = save?.Settings?.Clamped() ?? SaveSettings.Default;
        _sim = new Simulation(BalanceData.Load(json), save);

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

        // Offline progress: replay the time since the save was written. The
        // wall clock lives here in game/; the sim only ever sees a duration.
        if (save != null && save.TimestampUtcSeconds > 0)
        {
            double elapsed = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                - save.TimestampUtcSeconds;
            if (elapsed >= MinOfflineSeconds)
                AddChild(new AwayReport(OfflineCalculator.Run(_sim, elapsed)));
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
        // OS slept / laptop lid closed mid-session: route the gap through the
        // offline calculator instead of catch-up ticking (which would fast-
        // forward on screen and double-pay time the report already paid).
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - _lastUnix >= MinOfflineSeconds)
        {
            AddChild(new AwayReport(OfflineCalculator.Run(_sim, now - _lastUnix)));
            _acc = 0;
        }
        _lastUnix = now;

        // Live catch-up is capped at 1 s; anything longer is offline territory.
        _acc = Mathf.Min(_acc + delta, 1.0);
        double dt = _sim.Balance.TickSeconds;
        while (_acc >= dt)
        {
            _acc -= dt;
            List<ISimEvent> events = _sim.Tick();
            _battlefield?.OnSimEvents(events);
        }
        _hud.Refresh();

        _saveAcc += delta;
        if (_saveAcc >= 30.0) { _saveAcc = 0; SaveManager.Save(_sim.State, _settings); }

        if (_shotCountdown > 0 && --_shotCountdown == 0)
        {
            GetViewport().GetTexture().GetImage().SavePng(_shotPath);
            SaveManager.Save(_sim.State, _settings);
            GetTree().Quit();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            SaveManager.Save(_sim.State, _settings);
            GetTree().Quit();
        }
    }
}
