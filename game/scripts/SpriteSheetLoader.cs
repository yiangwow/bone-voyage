using Godot;

namespace BoneVoyage.Game;

public static class SpriteSheetLoader
{
    public static SpriteFrames Load(string basePath)
    {
        var tex = GD.Load<Texture2D>($"{basePath}.png");
        var meta = Json.ParseString(FileAccess.GetFileAsString($"{basePath}.meta.json"))
            .AsGodotDictionary();
        int fs = (int)meta["frameSize"].AsDouble();
        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");
        var anims = meta["animations"].AsGodotDictionary();
        foreach (var key in anims.Keys)
        {
            string name = key.AsString();
            var def = anims[key].AsGodotDictionary();
            int row = (int)def["row"].AsDouble();
            int count = (int)def["frames"].AsDouble();
            frames.AddAnimation(name);
            frames.SetAnimationSpeed(name, def["fps"].AsDouble());
            frames.SetAnimationLoop(name, def["loop"].AsBool());
            for (int i = 0; i < count; i++)
            {
                var at = new AtlasTexture
                {
                    Atlas = tex,
                    Region = new Rect2(i * fs, row * fs, fs, fs)
                };
                frames.AddFrame(name, at);
            }
        }
        return frames;
    }
}
