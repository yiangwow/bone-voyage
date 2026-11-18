using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class SkeletonActor : Node2D
{
    public SkeletonTypeId Type;
    public bool Walking { get; private set; } = true;
    private AnimatedSprite2D _sprite = null!;
    private float _speedPx;
    private float _stopX;

    public SkeletonActor(SkeletonTypeId type, SpriteFrames frames, float speedPx, float stopX)
    {
        Type = type;
        _speedPx = speedPx;
        _stopX = stopX;
        _sprite = new AnimatedSprite2D { SpriteFrames = frames, Scale = new Vector2(2, 2) };
        AddChild(_sprite);
        _sprite.Play("walk");
        _sprite.AnimationFinished += OnAnimFinished;
    }

    public override void _Process(double delta)
    {
        if (!Walking) return;
        var x = Position.X + _speedPx * (float)delta;
        Position = new Vector2(Mathf.Min(x, _stopX), Position.Y);
    }

    public void PlayDeath(string anim) // "die_wave" | "die_salute" | "whack"
    {
        if (!Walking) return;
        Walking = false;
        _sprite.Play(anim);
    }

    private void OnAnimFinished()
    {
        if (!Walking) QueueFree();
    }
}
