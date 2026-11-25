from PIL import Image, ImageDraw
import palette as P


def _img(w, h):
    im = Image.new("RGBA", (w, h), P.TRANSPARENT)
    return im, ImageDraw.Draw(im)


def _merlons(d, x0, y, x1, mw=4, gap=3, h=5, fill=P.STONE):
    """Crenellation: individual outlined merlon blocks sitting on top edge y,
    poking up by h, with real gaps between them (not just a wash of color)."""
    x = x0
    while x + mw <= x1 + 1:
        d.rectangle([x, y - h, x + mw, y + 1], fill=fill, outline=P.OUTLINE, width=2)
        x += mw + gap


def _brick_courses(d, x0, y0, x1, y1, row=6, col=8, dark=P.STONE_DK):
    """Offset masonry coursing: horizontal mortar lines plus staggered
    vertical joints so the stonework reads as brick, not a flat block."""
    row_i = 0
    for y in range(y0, y1, row):
        d.line([(x0 + 1, y), (x1 - 1, y)], fill=dark)
        off = col // 2 if row_i % 2 else 0
        for x in range(x0 + off, x1, col):
            d.line([(x, y), (x, min(y + row, y1))], fill=dark)
        row_i += 1


def tower(cannon=False):
    im, d = _img(24, 48)
    top, base_y = 12, 47

    # flared stone base (foundation reads wider than the shaft)
    d.rectangle([1, 40, 22, base_y], fill=P.STONE, outline=P.OUTLINE, width=2)
    # shaft
    d.rectangle([3, top, 20, 41], fill=P.STONE, outline=P.OUTLINE, width=2)
    _brick_courses(d, 4, top + 4, 19, 40)

    _merlons(d, 2, top, 21, mw=4, gap=2, h=5)

    if cannon:
        by = 24
        d.rectangle([2, by - 3, 15, by + 3], fill=P.STONE_DK, outline=P.OUTLINE, width=2)   # barrel shaft
        d.ellipse([0, by - 5, 9, by + 5], fill=P.STONE_DK, outline=P.OUTLINE, width=1)       # flared muzzle
        d.ellipse([2, by - 2, 5, by + 1], fill=P.OUTLINE)                                     # bore
        d.line([(4, by - 1), (12, by - 1)], fill=P.HELMET)                                    # metal sheen
    else:
        d.rounded_rectangle([10, 20, 13, 33], radius=1, fill=P.OUTLINE)

    return im


def wall():
    im, d = _img(32, 160)
    top = 10

    d.rectangle([1, top, 30, 159], fill=P.STONE, outline=P.OUTLINE, width=2)
    _brick_courses(d, 2, top + 5, 29, 159)

    _merlons(d, 0, top, 32, mw=5, gap=3, h=6)

    # gate near the base: dark archway recessed into the stonework
    gx0, gx1, gy0, gy1 = 9, 22, 118, 159
    d.rectangle([gx0, gy0 + 5, gx1, gy1], fill=P.OUTLINE)
    d.pieslice([gx0, gy0 - 6, gx1, gy0 + 16], 180, 360, fill=P.OUTLINE)
    d.rectangle([gx0 + 3, gy0 + 8, gx1 - 3, gy1], fill=P.STONE_DK)
    d.pieslice([gx0 + 3, gy0 - 2, gx1 - 3, gy0 + 16], 180, 360, fill=P.STONE_DK)

    return im


def necropolis():
    im, d = _img(64, 64)
    top = 20

    # dark keep silhouette
    d.rectangle([4, top, 59, 63], fill=P.STONE_DK, outline=P.OUTLINE, width=2)
    _brick_courses(d, 5, top + 5, 58, 62, dark=P.OUTLINE)
    _merlons(d, 3, top, 60, mw=5, gap=3, h=6, fill=P.STONE_DK)

    # side towers flanking the gate, taller than the curtain and seamed off
    # from it with a mortar line so they read as separate flanking towers
    for tx in (5, 47):
        d.rectangle([tx, top - 6, tx + 12, top + 3], fill=P.STONE_DK, outline=P.OUTLINE, width=2)
        _merlons(d, tx - 1, top - 6, tx + 13, mw=4, gap=2, h=4, fill=P.STONE_DK)
    d.line([(17, top - 6), (17, 63)], fill=P.OUTLINE)
    d.line([(47, top - 6), (47, 63)], fill=P.OUTLINE)

    # dark open gate archway (same language as the wall's gate)
    gx0, gx1, gy0 = 22, 41, 26
    d.rectangle([gx0, gy0 + 8, gx1, 63], fill=P.OUTLINE)
    d.pieslice([gx0, gy0 - 8, gx1, gy0 + 16], 180, 360, fill=P.OUTLINE)
    d.rectangle([gx0 + 3, gy0 + 11, gx1 - 3, 63], fill=P.STONE_DK)
    d.pieslice([gx0 + 3, gy0 - 5, gx1 - 3, gy0 + 16], 180, 360, fill=P.STONE_DK)

    # cute skull plaque mounted as a keystone above the arch, same friendly
    # face language as the marching skeletons (round head, dot eyes, grin)
    cx, cy = (gx0 + gx1) // 2, gy0 - 1
    d.ellipse([cx - 7, cy - 6, cx + 7, cy + 6], fill=P.BONE, outline=P.OUTLINE, width=2)
    d.ellipse([cx - 4, cy - 2, cx - 1, cy + 1], fill=P.EYE)
    d.ellipse([cx + 1, cy - 2, cx + 4, cy + 1], fill=P.EYE)
    d.line([(cx - 2, cy + 3), (cx + 2, cy + 3)], fill=P.EYE)

    # tiny punch-clock bolted to the wall beside the gate: round face with
    # hands, plus a card slot underneath so it reads as a time clock
    px, py = 49, 40
    d.rounded_rectangle([px - 2, py - 2, px + 10, py + 16], radius=2, fill=P.SIGN_WOOD, outline=P.OUTLINE, width=2)
    d.ellipse([px, py, px + 8, py + 8], fill=P.SIGN_FACE, outline=P.OUTLINE)
    d.line([(px + 4, py + 4), (px + 4, py + 1)], fill=P.OUTLINE)
    d.line([(px + 4, py + 4), (px + 6, py + 5)], fill=P.OUTLINE)
    d.rectangle([px + 1, py + 11, px + 7, py + 13], fill=P.OUTLINE)  # card slot

    return im


def tower_knight():
    """Melee-danger post: same stone base/shaft/merlons as the arrow and
    cannon towers, but topped with a sword driven in point-up and a knight
    helm leaning against it, so the skyline itself reads as 'melee' rather
    than needing a mid-shaft window or barrel."""
    im, d = _img(24, 48)
    top, base_y = 12, 47

    d.rectangle([1, 40, 22, base_y], fill=P.STONE, outline=P.OUTLINE, width=2)
    d.rectangle([3, top, 20, 41], fill=P.STONE, outline=P.OUTLINE, width=2)
    _brick_courses(d, 4, top + 4, 19, 40)
    _merlons(d, 2, top, 21, mw=4, gap=2, h=5)

    # a sword blade rises well above the battlements through the center
    # merlon -- a bold silhouette cue neither arrow (recessed slit) nor
    # cannon (side-mounted barrel) has, so the topper reads as "melee"
    # before the details are even legible
    cx = 11
    d.rectangle([cx - 1, 0, cx + 1, 13], fill=P.HELMET, outline=P.OUTLINE, width=1)
    d.line([(cx, 1), (cx, 12)], fill=P.HELMET_DK)                              # blade sheen
    d.rectangle([cx - 4, 13, cx + 5, 15], fill=P.OUTLINE)                      # crossguard
    d.rectangle([cx, 15, cx + 1, 18], fill=P.SIGN_WOOD, outline=P.OUTLINE)     # grip

    # knight helm mounted like a plaque below the hilt, sword driven down
    # through its crest -- same bolted-plaque language as necropolis's
    # skull keystone. A closed, horizontal eye slit (not a full-height
    # vertical split) so the dome reads as a face, not a bisected coin.
    d.rounded_rectangle([cx - 6, 19, cx + 6, 29], radius=3, fill=P.HELMET, outline=P.OUTLINE, width=2)
    d.rectangle([cx - 3, 23, cx + 3, 24], fill=P.OUTLINE)                     # closed eye slit
    d.line([(cx, 19), (cx, 21)], fill=P.OUTLINE)                              # short crest ridge, ties to hilt above
    d.point([(cx - 3, 21)], fill=P.HELMET_DK)                                 # brim shading

    return im


def moat():
    """24x24 water tile: flat WATER base with a couple of WATER_DK ripple
    bands and a few lighter SKY glints so it isn't a dead flat fill."""
    im, d = _img(24, 24)
    d.rectangle([0, 0, 23, 23], fill=P.WATER)

    for y in (6, 16):
        d.line([(0, y), (5, y + 2), (11, y), (17, y + 2), (23, y)], fill=P.WATER_DK, width=2)

    for pt in [(4, 5), (14, 15), (20, 6)]:
        d.point([pt], fill=P.SKY)

    return im


def flag():
    """16x16 conquest-strip flag: same pole + ACCENT pennant language as
    bannerbones' banner pole, transparent background."""
    im, d = _img(16, 16)
    x, y_top, y_bottom = 4, 1, 14

    d.line([(x, y_top), (x, y_bottom)], fill=P.OUTLINE, width=3)
    d.line([(x, y_top), (x, y_bottom)], fill=P.SIGN_WOOD, width=2)
    d.rectangle([x, y_top, x + 8, y_top + 5], fill=P.ACCENT, outline=P.OUTLINE)

    return im


def punchclock():
    """Free-standing shift clock by the necropolis gate: post, SIGN_WOOD
    housing, SIGN_FACE dial with OUTLINE hands, card slot — the big sibling of
    the tiny clock bolted to the necropolis wall."""
    im, d = _img(16, 28)
    d.line([(8, 18), (8, 27)], fill=P.OUTLINE, width=3)
    d.line([(8, 18), (8, 27)], fill=P.SIGN_WOOD, width=2)
    d.rounded_rectangle([2, 1, 14, 19], radius=2, fill=P.SIGN_WOOD, outline=P.OUTLINE, width=2)
    d.ellipse([4, 3, 12, 11], fill=P.SIGN_FACE, outline=P.OUTLINE)
    d.line([(8, 7), (8, 4)], fill=P.OUTLINE)
    d.line([(8, 7), (10, 8)], fill=P.OUTLINE)
    d.rectangle([4, 14, 12, 16], fill=P.OUTLINE)  # card slot
    return im


def emit_all(emit):
    """emit(name, sheet, meta) from generate.py; meta unused for props."""
    for name, im in [("tower_arrow", tower(False)), ("tower_cannon", tower(True)),
                     ("wall", wall()), ("necropolis", necropolis()),
                     ("tower_knight", tower_knight()), ("moat", moat()), ("flag", flag()),
                     ("punchclock", punchclock())]:
        emit(name, im, {"frameSize": 0, "animations": {}})
