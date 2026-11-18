import json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import skeletons

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "game", "assets", "sprites")
PREVIEW = os.path.join(os.path.dirname(__file__), "preview")

def emit(name, sheet, meta):
    os.makedirs(OUT, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
    sheet.save(os.path.join(OUT, f"{name}.png"))
    if meta.get("animations"):
        with open(os.path.join(OUT, f"{name}.meta.json"), "w") as f:
            json.dump(meta, f, indent=2)
    w, h = sheet.size
    sheet.resize((w * 8, h * 8), resample=0).save(os.path.join(PREVIEW, f"{name}@8x.png"))
    print(f"wrote {name}: {w}x{h}")

if __name__ == "__main__":
    for name, variant in [
        ("marcher", "marcher"),
        ("shieldbearer", "shieldbearer"),
        ("sprinter", "sprinter"),
        ("bomber", "bomber"),
        ("bannerbones", "bannerbones"),
    ]:
        sheet, meta = skeletons.build_sheet(variant)
        emit(name, sheet, meta)
    try:
        import props
        props.emit_all(emit)
        print("props done")
    except ImportError:
        print("props.py not present yet (Task 8)")
