from PIL import Image, ImageDraw
import palette as P

F = 24  # frame size


def _frame():
    img = Image.new("RGBA", (F, F), P.TRANSPARENT)
    return img, ImageDraw.Draw(img)


def _bone_limb(d, x0, y0, x1, y1, w=1, cap=1):
    """A thin jointed bone segment: outlined line with a small knobby joint
    cap at the far end. Kept thin so limbs read as bones, not sausages, and
    so their outline halo doesn't smudge into the torso they attach to."""
    d.line([(x0, y0), (x1, y1)], fill=P.OUTLINE, width=w + 2)
    d.line([(x0, y0), (x1, y1)], fill=P.BONE, width=w)
    d.ellipse([x1 - cap, y1 - cap, x1 + cap, y1 + cap], fill=P.BONE, outline=P.OUTLINE)


def _head(d, cx, cy, r=4, front=False, eyes="open"):
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=P.BONE, outline=P.OUTLINE)
    if front:
        if eyes == "closed":  # proud little ^-^ lines
            d.line([(cx - 3, cy - 1), (cx - 1, cy - 1)], fill=P.EYE)
            d.line([(cx + 1, cy - 1), (cx + 3, cy - 1)], fill=P.EYE)
        else:
            d.point([(cx - 2, cy - 1), (cx + 2, cy - 1)], fill=P.EYE)
        d.point([(cx - 1, cy + 1), (cx, cy + 2), (cx + 1, cy + 1)], fill=P.EYE)  # smile
    else:
        # side profile: one eye dot + tiny grin, facing right, kept well
        # inside the head silhouette so it doesn't merge with the outline ring
        d.point([(cx + 2, cy - 1)], fill=P.EYE)
        d.line([(cx, cy + 2), (cx + 2, cy + 2)], fill=P.EYE)


def _torso(d, cx, top, bottom, half_w=2):
    d.ellipse([cx - half_w, top, cx + half_w, bottom], fill=P.BONE, outline=P.OUTLINE)
    mid = (top + bottom) // 2
    d.line([(cx - half_w + 1, mid), (cx + half_w - 1, mid)], fill=P.BONE_SHADE)


def _ground_shadow(d, cx, y):
    d.ellipse([cx - 6, y, cx + 6, y + 1], fill=P.BONE_SHADE)


def _shield(d, x, y, w=5, h=8):
    d.rounded_rectangle([x, y, x + w, y + h], radius=2, fill=P.SHIELD, outline=P.SHIELD_RIM)
    cx, cy = x + w // 2, y + h // 2
    d.line([(cx, y + 1), (cx, y + h - 1)], fill=P.SHIELD_RIM)
    d.line([(x + 1, cy), (x + w - 1, cy)], fill=P.SHIELD_RIM)


def _helmet(d, cx, cy, r=4):
    d.arc([cx - r - 1, cy - r - 1, cx + r + 1, cy + 1], 180, 360, fill=P.HELMET, width=2)
    d.point([(cx - r - 1, cy - 1), (cx + r + 1, cy - 1)], fill=P.HELMET_DK)


def _scarf(d, cx, cy, i):
    """Sprinter's trailing scarf: a short ribbon knotted behind the neck
    that flaps to a different position each frame, selling forward motion
    even when the body silhouette barely changes."""
    tails = [(cx - 6, cy + 2), (cx - 8, cy), (cx - 6, cy - 2), (cx - 8, cy + 1)]
    tail = tails[i % 4]
    d.line([(cx - 2, cy - 1), tail], fill=P.OUTLINE, width=3)
    d.line([(cx - 2, cy - 1), tail], fill=P.SCARF, width=2)


def _bomb(d, x, y, lit=True):
    """A round dark bomb: an OUTLINE-filled circle with a small highlight
    (so it reads as round, not a flat dot) and an optional lit SPARK fuse."""
    r = 2
    d.ellipse([x - r, y - r, x + r, y + r], fill=P.OUTLINE, outline=P.OUTLINE)
    d.point([(x - 1, y - 1)], fill=P.HELMET)
    if lit:
        d.line([(x + 1, y - r), (x + 2, y - r - 2)], fill=P.SIGN_WOOD)
        d.point([(x + 2, y - r - 3)], fill=P.SPARK)


def _banner(d, x, y_top, y_bottom, fallen=False):
    """A vertical banner pole topped with an ACCENT pennant; when the
    bearer falls, the same pole is drawn lying across the bone pile."""
    if fallen:
        d.line([(x - 7, y_top), (x + 7, y_top)], fill=P.OUTLINE, width=3)
        d.line([(x - 7, y_top), (x + 7, y_top)], fill=P.SIGN_WOOD, width=2)
        d.rectangle([x - 6, y_top - 4, x - 1, y_top - 1], fill=P.ACCENT, outline=P.OUTLINE)
    else:
        d.line([(x, y_top), (x, y_bottom)], fill=P.OUTLINE, width=3)
        d.line([(x, y_top), (x, y_bottom)], fill=P.SIGN_WOOD, width=2)
        d.rectangle([x, y_top, x + 5, y_top + 4], fill=P.ACCENT, outline=P.OUTLINE)


def walk_frame(i, variant):
    """4-frame walk cycle, marching right with a cheerful head bob and a
    clear leg-scissor stride (front/back legs visibly separated at ground).
    `variant` picks the character skin: marcher/shieldbearer/sprinter/
    bomber/bannerbones."""
    img, d = _frame()
    bob = [0, -1, 0, -1][i]
    cx = 11
    hip_y = 16 + bob
    ground = 21
    lean = variant == "sprinter"

    _ground_shadow(d, cx, ground)

    # leg poses: (dx_far_foot_from_hip_x, foot_y) for the two legs each frame,
    # scissoring so front/back legs are always visibly apart at the ground.
    # sprinter gets a wider stride so it reads as leaner/faster at a glance.
    stride = 5 if lean else 3
    leg_poses = [
        ((cx + stride, ground - 1), (cx - stride, ground)),   # right leg forward+up, left leg back
        ((cx + 1, ground), (cx - 1, ground)),                 # passing, close together
        ((cx - stride, ground), (cx + stride, ground - 1)),   # left leg forward+up, right leg back
        ((cx + 1, ground), (cx - 1, ground)),                 # passing, close together
    ][i]
    hips = [(cx - 1, hip_y), (cx + 1, hip_y)]
    for (hx, hy), (fx, fy) in zip(hips, leg_poses):
        _bone_limb(d, hx, hy, fx, fy, w=1, cap=1)

    half_w = 2 if lean else 3
    _torso(d, cx, 9 + bob, hip_y, half_w=half_w)

    if variant == "bomber":
        # steady two-handed carry instead of a free swing, since the arm
        # is loaded with a live bomb
        hand = (cx + 5, 12 + bob)
        _bone_limb(d, cx + 2, 11 + bob, *hand, w=1, cap=1)
        _bomb(d, hand[0] + 1, hand[1] - 1)
    else:
        # single visible (near) arm, swinging opposite the near leg
        arm_tip = [(cx - 3, 14 + bob), (cx - 1, 15 + bob), (cx + 4, 12 + bob), (cx - 1, 15 + bob)][i]
        _bone_limb(d, cx + 2, 11 + bob, *arm_tip, w=1, cap=1)

    _head(d, cx, 5 + bob)

    if variant == "shieldbearer":
        _helmet(d, cx, 5 + bob)
        _shield(d, cx + 4, 8 + bob, w=4, h=7)
    elif variant == "sprinter":
        _scarf(d, cx, 7 + bob, i)
    elif variant == "bannerbones":
        _banner(d, cx + 7, 1 + bob, ground - 1)

    return img


def _bone_pile(d, stage, variant=None, exploded=False):
    """stage 0: slumping to the ground; 1: cheerful seated pile; 2: flat
    pile + sparkles. At stage 0 a character-specific prop is left behind:
    shieldbearer's shield, bannerbones' fallen pole, or bomber's unexploded
    bomb (skipped when `exploded`, since the bomb went off with the row)."""
    cx = 11
    if stage == 0:
        d.rounded_rectangle([6, 12, 16, 20], radius=3, fill=P.BONE_SHADE, outline=P.OUTLINE)
        _head(d, cx, 11, front=True)
        d.point([(cx - 3, 12), (cx + 3, 12)], fill=P.BLUSH)
    elif stage == 1:
        d.ellipse([5, 15, 17, 22], fill=P.BONE_SHADE, outline=P.OUTLINE)
        _head(d, cx, 14, front=True)
        d.point([(cx - 3, 15), (cx + 3, 15)], fill=P.BLUSH)
        d.point([(cx - 4, 19), (cx + 4, 19), (cx - 2, 20), (cx + 2, 20)], fill=P.BONE)
    else:
        # a cute little skull-and-crossbones heap: low mound, two crossed
        # bones with round knobby ends, tiny skull resting on top
        d.ellipse([4, 20, 18, 23], fill=P.BONE_SHADE, outline=P.OUTLINE)
        for x0, y0, x1, y1 in [(6, 21, 16, 17), (6, 17, 16, 21)]:
            d.line([(x0, y0), (x1, y1)], fill=P.OUTLINE, width=3)
            d.line([(x0, y0), (x1, y1)], fill=P.BONE, width=1)
            for ex, ey in [(x0, y0), (x1, y1)]:
                d.ellipse([ex - 1, ey - 1, ex + 1, ey + 1], fill=P.BONE, outline=P.OUTLINE)
        d.ellipse([cx - 2, 14, cx + 2, 18], fill=P.BONE, outline=P.OUTLINE)
        d.point([(cx - 1, 16), (cx + 1, 16)], fill=P.EYE)
        d.point([(3, 13), (19, 11), (10, 8), (21, 16), (5, 6)], fill=P.SPARK)

    if stage == 0:
        if variant == "bannerbones":
            _banner(d, 11, 17, None, fallen=True)
        elif variant == "bomber" and not exploded:
            _bomb(d, 4, 15, lit=False)


def _sign(d, cx, top_y):
    """'see you tomorrow!' picket sign: SIGN_WOOD post, SIGN_FACE board with
    EYE squiggle writing (24 px can't render letters; two scribble lines and a
    dot read as a cheerful note). Sized and positioned by the caller to clear
    the head and stay inside the 24px frame."""
    d.line([(cx, top_y + 6), (cx, top_y + 13)], fill=P.OUTLINE, width=3)
    d.line([(cx, top_y + 6), (cx, top_y + 13)], fill=P.SIGN_WOOD, width=2)
    d.rectangle([cx - 5, top_y, cx + 5, top_y + 6], fill=P.SIGN_FACE, outline=P.OUTLINE)
    d.line([(cx - 3, top_y + 2), (cx + 3, top_y + 2)], fill=P.EYE)
    d.line([(cx - 3, top_y + 4), (cx + 1, top_y + 4)], fill=P.EYE)
    d.point([(cx + 3, top_y + 4)], fill=P.EYE)


def sign_frame(i, variant):
    """6 frames: 0 sign at chest height, 1 hoisted, 2 held high with sparkles,
    3-5 crumble with the sign left planted beside the pile. The body shifts
    left of its usual center on frames 0-2 so the (wide) board has room to
    sit clear of the head instead of covering the face."""
    img, d = _frame()
    cx = 11
    lean = variant == "sprinter"
    half_w = 2 if lean else 3
    if i <= 2:
        bx = 8
        _ground_shadow(d, cx, 21)
        _bone_limb(d, bx - 1, 16, bx - 3, 21, w=1, cap=1)
        _bone_limb(d, bx + 1, 16, bx + 2, 21, w=1, cap=1)
        _torso(d, bx, 9, 16, half_w=half_w)
        sign_top = [12, 4, 0][i]
        hold_x = 17
        _bone_limb(d, bx + 2, 11, hold_x, sign_top + 6, w=1, cap=1)
        if variant == "bomber":
            # the sign only merits one hand; the bomb stays cradled in the other
            hand = (bx - 4, 13)  # nudged 1px right of the bare-minimum hand pose for edge margin
            _bone_limb(d, bx - 2, 11, *hand, w=1, cap=1)
            _bomb(d, hand[0] - 1, hand[1])
        elif i >= 1:
            _bone_limb(d, bx - 2, 11, hold_x - 3, sign_top + 8, w=1, cap=1)  # both hands
        if variant == "shieldbearer":
            _shield(d, 1, 11, w=4, h=7)  # strapped to the free side, clear of legs/torso and the frame edge
        _head(d, bx, 5, front=True)
        d.point([(bx - 3, 6), (bx + 3, 6)], fill=P.BLUSH)
        if variant == "shieldbearer":
            _helmet(d, bx, 5)
        elif variant == "sprinter":
            _scarf(d, bx, 7, i)
        elif variant == "bannerbones":
            _banner(d, bx - 6, 10, 21)  # pole moves to the free side, pennant dropped beside the torso, clear of the head
        _sign(d, hold_x, sign_top)      # drawn last: sign reads in front
        if i == 2:
            d.point([(bx - 5, 2), (hold_x + 5, 2), (bx - 2, 0)], fill=P.SPARK)
    else:
        _bone_pile(d, i - 3, variant)
        if variant == "shieldbearer" and i == 3:
            _shield(d, 2, 14, w=4, h=6)
        _sign(d, 16, 8 + (i - 3))       # planted beside the pile, settling
    return img


def die_frame(i, gesture, variant):
    """6 frames: 0 face camera, 1-2 gesture (wave or salute), 3-5 cheerful
    crumble. `gesture` picks wave/salute; `variant` picks the character
    skin."""
    img, d = _frame()
    cx = 11
    lean = variant == "sprinter"
    half_w = 2 if lean else 3

    if i <= 2:
        _ground_shadow(d, cx, 21)
        if gesture == "wave":
            # loose goodbye: body rocks, near arm swings a big metronome arc,
            # far arm joins from frame 1 — an unmistakable two-hand wave
            rock = [0, 1, -1][i]
            bx = cx + rock
            _bone_limb(d, bx - 1, 16, cx - 4, 21, w=1, cap=1)   # legs splayed, planted
            _bone_limb(d, bx + 1, 16, cx + 3, 21, w=1, cap=1)
            _torso(d, bx, 9, 16, half_w=half_w)
            near = [(cx + 5, 11), (cx + 8, 5), (cx + 2, 2)][i]
            _bone_limb(d, bx + 2, 10, *near, w=1, cap=1)
            far = [None, (cx - 7, 8), (cx - 4, 2)][i]
            if far is None:
                if variant == "bomber":
                    hand = (bx - 5, 13)
                    _bone_limb(d, bx - 2, 11, *hand, w=1, cap=1)
                    _bomb(d, hand[0] - 1, hand[1])
                else:
                    _bone_limb(d, bx - 2, 11, bx - 4, 15, w=1, cap=1)
            else:
                _bone_limb(d, bx - 2, 10, *far, w=1, cap=1)
                if variant == "bomber":
                    _bomb(d, far[0] - 1, far[1] - 2)   # bomb rides the waving hand
            head_x, head_eyes = bx, "open"
        else:
            # formal sign-off: heels snap together, spine straight, free arm
            # pinned to the side, crisp brow salute held with a glint
            bx = cx
            _bone_limb(d, cx - 1, 16, cx - 1, 21, w=1, cap=1)   # legs vertical, together
            _bone_limb(d, cx + 1, 16, cx + 1, 21, w=1, cap=1)
            _torso(d, cx, 9, 16, half_w=half_w)
            if variant == "bomber":
                hand = (cx - 4, 14)
                _bone_limb(d, cx - 2, 11, *hand, w=1, cap=1)
                _bomb(d, hand[0] - 1, hand[1])
            else:
                _bone_limb(d, cx - 2, 11, cx - 3, 16, w=1, cap=1)  # arm at the side
            tip = [(cx + 4, 12), (cx + 6, 3), (cx + 5, 3)][i]
            _bone_limb(d, cx + 2, 10, *tip, w=1, cap=1)
            if i == 2:
                d.point([(cx + 7, 2), (cx + 8, 4)], fill=P.SPARK)  # glint on the held salute
            head_x, head_eyes = cx, ("closed" if i >= 1 else "open")

        if variant == "shieldbearer":
            _shield(d, bx - 10, 11, w=4, h=7)
        _head(d, head_x, 5, front=True, eyes=head_eyes)
        d.point([(head_x - 3, 6), (head_x + 3, 6)], fill=P.BLUSH)
        if variant == "shieldbearer":
            _helmet(d, head_x, 5)
        elif variant == "sprinter":
            _scarf(d, head_x, 7, i)
        elif variant == "bannerbones":
            _banner(d, bx + 7, 1, 20)
    else:
        _bone_pile(d, i - 3, variant)
        if variant == "shieldbearer" and i == 3:
            _shield(d, 2, 14, w=4, h=6)

    return img


def _explosion(d, cx, cy):
    """A big starburst that fully replaces the skeleton silhouette: bright
    SPARK core, radiating SPARK/BLUSH rays, and BONE shrapnel flying
    outward — reads as a bomb going off, not a skeleton anymore."""
    r = 4
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=P.SPARK, outline=P.OUTLINE)
    rays = [(0, -9), (0, 9), (-9, 0), (9, 0), (-7, -7), (7, -7), (-7, 7), (7, 7)]
    for n, (dx, dy) in enumerate(rays):
        color = P.SPARK if n % 2 == 0 else P.BLUSH
        d.line([(cx, cy), (cx + dx, cy + dy)], fill=color, width=1)
    shrapnel = [(cx - 10, cy - 3), (cx + 10, cy - 2), (cx - 9, cy + 6),
                (cx + 8, cy + 7), (cx - 3, cy - 11), (cx + 4, cy + 11)]
    d.point(shrapnel, fill=P.BONE)


def _bomb_boom_frame(i):
    """Bomber's whack row: 0 place bomb at the wall side, 1 crouch with a
    lit fuse, 2 the explosion (skeleton fully obscured), 3-5 a bone pile
    with drifting sparks and no bomb left (it went off)."""
    img, d = _frame()
    cx = 11

    if i == 0:
        _ground_shadow(d, cx, 21)
        _bone_limb(d, cx - 1, 16, cx - 2, 21, w=1, cap=1)
        _bone_limb(d, cx + 1, 16, cx + 3, 20, w=1, cap=1)
        _torso(d, cx, 10, 16, half_w=3)
        _bone_limb(d, cx + 2, 12, cx + 8, 18, w=1, cap=1)
        _bomb(d, 18, 19, lit=False)
        _head(d, cx, 6)
    elif i == 1:
        _ground_shadow(d, cx, 21)
        _bone_limb(d, cx - 1, 17, cx - 3, 21, w=1, cap=1)
        _bone_limb(d, cx + 1, 17, cx + 2, 21, w=1, cap=1)
        _torso(d, cx, 13, 17, half_w=3)
        _bone_limb(d, cx - 2, 14, cx - 6, 17, w=1, cap=1)
        _bomb(d, 17, 18, lit=True)
        _head(d, cx, 9)
    elif i == 2:
        _explosion(d, cx, 12)
    else:
        _bone_pile(d, i - 3, variant="bomber", exploded=True)
        drift = [[(3, 6), (20, 9), (14, 4)],
                 [(6, 3), (18, 6), (10, 2)],
                 [(2, 10), (21, 5), (16, 8)]][i - 3]
        d.point(drift, fill=P.SPARK)

    return img


def whack_frame(i, variant):
    """6 frames: 0 windup, 1 swing, 2 impact stars, 3-5 crumble. Bomber
    swaps its whole row for a bomb-boom sequence (see _bomb_boom_frame)."""
    if variant == "bomber":
        return _bomb_boom_frame(i)

    img, d = _frame()
    cx = 11
    lean = variant == "sprinter"
    half_w = 2 if lean else 3

    # cx stays fixed so the arm arc (back -> overhead -> forward) reads
    # clearly against a stable body instead of a drifting silhouette
    if i == 0:
        _ground_shadow(d, cx, 21)
        _bone_limb(d, cx - 1, 16, cx - 3, 21, w=1, cap=1)
        _bone_limb(d, cx + 1, 16, cx, 21, w=1, cap=1)
        _torso(d, cx, 9, 16, half_w=half_w)
        # arm cocked back and low, well clear of the head (head spans x 7..15)
        _bone_limb(d, cx - 2, 12, cx - 7, 15, w=1, cap=1)
        _head(d, cx, 5)
        if variant == "bannerbones":
            _banner(d, cx + 7, 1, 20)
        elif variant == "sprinter":
            _scarf(d, cx, 7, i)
    elif i == 1:
        _ground_shadow(d, cx, 21)
        _bone_limb(d, cx - 1, 16, cx - 2, 21, w=1, cap=1)
        _bone_limb(d, cx + 1, 16, cx + 1, 21, w=1, cap=1)
        _torso(d, cx, 9, 16, half_w=half_w)
        # arm swung up overhead, mid-swing
        _bone_limb(d, cx + 2, 11, cx + 6, 3, w=1, cap=1)
        _head(d, cx, 5)
        if variant == "bannerbones":
            _banner(d, cx + 7, 1, 20)
        elif variant == "sprinter":
            _scarf(d, cx, 7, i)
    elif i == 2:
        _ground_shadow(d, cx, 21)
        _bone_limb(d, cx - 1, 16, cx - 1, 21, w=1, cap=1)
        _bone_limb(d, cx + 1, 16, cx + 2, 21, w=1, cap=1)
        _torso(d, cx, 9, 16, half_w=half_w)
        # follow-through: arm extended forward-down at impact height, sparks at the tip
        _bone_limb(d, cx + 2, 11, cx + 9, 10, w=1, cap=1)
        d.point([(cx + 10, 6), (cx + 12, 9), (cx + 11, 13), (cx + 9, 4), (cx + 13, 7)], fill=P.SPARK)
        _head(d, cx, 5)
        if variant == "sprinter":
            _scarf(d, cx, 7, i)
    else:
        _bone_pile(d, i - 3, variant)
        d.point([(19, 8), (21, 12), (20, 5)], fill=P.SPARK)

    return img


ANIMS = {
    "walk":       {"row": 0, "frames": 4, "fps": 8,  "loop": True,  "fn": walk_frame},
    "die_wave":   {"row": 1, "frames": 6, "fps": 9,  "loop": False, "fn": lambda i, v: die_frame(i, "wave", v)},
    "die_salute": {"row": 2, "frames": 6, "fps": 9,  "loop": False, "fn": lambda i, v: die_frame(i, "salute", v)},
    "whack":      {"row": 3, "frames": 6, "fps": 10, "loop": False, "fn": whack_frame},
    "die_sign":   {"row": 4, "frames": 6, "fps": 8,  "loop": False, "fn": sign_frame},
}


def build_sheet(variant):
    """variant in {"marcher", "shieldbearer", "sprinter", "bomber", "bannerbones"}."""
    cols = max(a["frames"] for a in ANIMS.values())
    sheet = Image.new("RGBA", (cols * F, len(ANIMS) * F), P.TRANSPARENT)
    for a in ANIMS.values():
        for i in range(a["frames"]):
            frame = a["fn"](i, variant)
            sheet.paste(frame, (i * F, a["row"] * F))
    meta = {"frameSize": F, "animations": {
        k: {kk: v[kk] for kk in ("row", "frames", "fps", "loop")} for k, v in ANIMS.items()}}
    return sheet, meta
