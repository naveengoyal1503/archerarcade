#!/usr/bin/env python3
"""
Arena composition preview without Unity: draws the scenes exported by Tools/ScenePreview (real Logic layouts) with the
game's atlases, following the Runtime placement rules (ArenaView, PropView, ParallaxLayer, SkyLayer, CameraRig's aim
framing). Characters use the idle pose pictures scaled to the rig height. Output: one PNG per scene at 1688 × 780
(the 844 × 390 reference × 2) in the scratch folder given as the second argument.

  dotnet run --project Tools/ScenePreview > scenes.json
  python Tools/art/preview_arena.py scenes.json out_dir
"""
import json
import math
import os
import random
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ART = os.path.join(ROOT, "Assets", "ArcherArcade", "Resources", "Art")
W, H = 1688, 780
ASPECT = W / H
GROUND_SHARE = 74 / 390
_atlas = {}


def atlas(group):
    if group not in _atlas:
        base = os.path.join(ART, "Sprites", group) if group not in ("Poses",) else os.path.join(ART, group)
        _atlas[group] = (Image.open(base + ".png").convert("RGBA"), json.load(open(base + ".json"))["sprites"])
    return _atlas[group]


class Sprite:
    def __init__(self, group, name):
        img, sprites = atlas(group)
        d = sprites[name]
        self.img = img.crop((int(d["x"]), int(d["y"]), int(d["x"] + d["w"]), int(d["y"] + d["h"])))
        self.ppm = d.get("ppm", 100)
        self.px, self.py = d["px"], d["py"]
        self.w, self.h = d["w"], d["h"]
        self.border = d.get("border")

    @property
    def width_m(self):
        return self.w / self.ppm

    @property
    def height_m(self):
        return self.h / self.ppm

    @property
    def min_y_m(self):  # bounds.min.y relative to the pivot (metres)
        return -(self.h - self.py) / self.ppm

    @property
    def max_y_m(self):
        return self.py / self.ppm


class Cam:
    def __init__(self, cx, cy, half_w):
        self.cx, self.cy, self.hw = cx, cy, half_w
        self.size = half_w / ASPECT
        self.ppm = W / (2 * half_w)

    def px(self, x, y):
        return ((x - (self.cx - self.hw)) * self.ppm, H - (y - (self.cy - self.size)) * self.ppm)


def frame(min_x, max_x, ground, top, focus=None, margin=1.2):
    hw = max(9.2, (max_x - min_x) / 2 + margin)
    need_h = (top + 0.8 - ground) / (1 - GROUND_SHARE)
    hw = max(hw, need_h * 0.5 * ASPECT)
    cx = (min_x + max_x) / 2
    if hw > 24:
        hw = 24
        if focus is not None:
            cx = min(max(focus, min_x + hw - margin), max_x - hw + margin)
    size = hw / ASPECT
    cy = ground - size * 2 * GROUND_SHARE + size
    return Cam(cx, cy, hw)


LAYERS = []  # (order, callable)


def put(order, fn):
    LAYERS.append((order, len(LAYERS), fn))


def tint(img, color):
    if color is None:
        return img
    r, g, b, a = img.split()
    cr, cg, cb = color[:3]
    alpha = color[3] if len(color) > 3 else 1.0
    r = r.point(lambda v: int(v * cr))
    g = g.point(lambda v: int(v * cg))
    b = b.point(lambda v: int(v * cb))
    a = a.point(lambda v: int(v * alpha))
    return Image.merge("RGBA", (r, g, b, a))


def draw_sprite(canvas, cam, spr, x, y, scale_x=1.0, scale_y=None, rot=0.0, flip=False, color=None):
    """Draws a simple sprite with its pivot at (x, y) world, scaled (world size = sprite size × scale)."""
    if scale_y is None:
        scale_y = scale_x
    pw = max(1, int(round(spr.w / spr.ppm * abs(scale_x) * cam.ppm)))
    ph = max(1, int(round(spr.h / spr.ppm * abs(scale_y) * cam.ppm)))
    img = spr.img.resize((pw, ph), Image.LANCZOS)
    pivx = spr.px / spr.w * pw
    pivy = spr.py / spr.h * ph
    if flip:
        img = img.transpose(Image.FLIP_LEFT_RIGHT)
        pivx = pw - pivx
    img = tint(img, color)
    if rot:
        big = Image.new("RGBA", (pw * 3, ph * 3), (0, 0, 0, 0))
        big.alpha_composite(img, (int(pw * 1.5 - pivx), int(ph * 1.5 - pivy)))
        img = big.rotate(rot, resample=Image.BICUBIC)
        pivx, pivy = pw * 1.5, ph * 1.5
    sx, sy = cam.px(x, y)
    canvas.alpha_composite(img, (int(sx - pivx), int(sy - pivy))) if -img.size[0] < sx - pivx < W and -img.size[1] < sy - pivy < H else None


def nine_slice(spr, width_px, height_px):
    b = spr.border or [0, 0, 0, 0]  # left, bottom, right, top (Unity order)
    l, bo, r, t = [int(v) for v in b]
    src = spr.img
    sw, sh = src.size
    out = Image.new("RGBA", (max(1, width_px), max(1, height_px)), (0, 0, 0, 0))
    scale = spr.ppm  # border px at sprite density; destination px per metre handled by caller
    return out if width_px <= 0 or height_px <= 0 else _slice(src, out, l, bo, r, t)


def _slice(src, out, l, bo, r, t):
    sw, sh = src.size
    ow, oh = out.size
    k = getattr(out, "_k", 1.0)
    L, R, T, B = int(l * k), int(r * k), int(t * k), int(bo * k)
    L = min(L, ow // 2); R = min(R, ow // 2); T = min(T, oh // 2); B = min(B, oh // 2)
    xs = [(0, l, 0, L), (l, sw - r, L, ow - R), (sw - r, sw, ow - R, ow)]
    ys = [(0, t, 0, T), (t, sh - bo, T, oh - B), (sh - bo, sh, oh - B, oh)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            part = src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS)
            out.alpha_composite(part, (dx0, dy0))
    return out


def draw_sliced(canvas, cam, spr, x, y, w_m, h_m, rot=0.0, color=None):
    """Sliced sprite of w × h metres with its pivot at (x, y) (pivot kept proportionally, like Unity)."""
    pw, ph = int(w_m * cam.ppm), int(h_m * cam.ppm)
    if pw < 1 or ph < 1:
        return
    out = Image.new("RGBA", (pw, ph), (0, 0, 0, 0))
    out._k = cam.ppm / spr.ppm
    img = _slice(spr.img, out, *(spr.border or [0, 0, 0, 0])) if spr.border else spr.img.resize((pw, ph), Image.LANCZOS)
    img = tint(img, color)
    pivx = spr.px / spr.w * pw
    pivy = spr.py / spr.h * ph
    if rot:
        big = Image.new("RGBA", (pw * 3, ph * 3), (0, 0, 0, 0))
        big.alpha_composite(img, (int(pw * 1.5 - pivx), int(ph * 1.5 - pivy)))
        img = big.rotate(rot, resample=Image.BICUBIC)
        pivx, pivy = pw * 1.5, ph * 1.5
    sx, sy = cam.px(x, y)
    canvas.alpha_composite(img, (int(sx - pivx), int(sy - pivy)))


def draw_tiled(canvas, cam, spr, x, y, w_m, h_m, color=None):
    pw, ph = max(1, int(w_m * cam.ppm)), max(1, int(h_m * cam.ppm))
    tile_h = max(1, int(spr.h / spr.ppm * cam.ppm * (w_m / spr.width_m)))
    tile = spr.img.resize((pw, tile_h), Image.LANCZOS)
    out = Image.new("RGBA", (pw, ph), (0, 0, 0, 0))
    for yy in range(0, ph, tile_h):
        out.alpha_composite(tile.crop((0, 0, pw, min(tile_h, ph - yy))), (0, yy))
    out = tint(out, color)
    pivx = spr.px / spr.w * pw
    pivy = spr.py / spr.h * ph
    sx, sy = cam.px(x, y)
    canvas.alpha_composite(out, (int(sx - pivx), int(sy - pivy)))


SKY = {
    "Day": ((0x8F, 0xD3, 0xFF), (0xE6, 0xF6, 0xFF), (1, 1, 1), (0.82, 0.93, 1.0), False),
    "Dusk": ((0x6D, 0x5B, 0xD0), (0xFF, 0xB3, 0x8A), (1, 0.88, 0.8), (0.86, 0.62, 0.72), False),
    "Night": ((0x15, 0x12, 0x38), (0x3A, 0x27, 0x66), (0.62, 0.64, 0.88), (0.3, 0.3, 0.55), True),
}


def render(scene, out_dir):
    LAYERS.clear()
    rnd = random.Random(7)
    top_c, bot_c, world, far, night = SKY[scene["time"]]
    fighters = scene["fighters"]
    me = next(f for f in fighters if f["side"] == 0 and f["active"])
    sx, feet = me["feet"]
    min_x, max_x, ground, top = sx - 2.2, sx + 2.2, feet, feet + 2.1 * me["scale"]
    foes = [f for f in fighters if f["side"] == 1 and f["active"]]
    focus = min_x + 9
    if foes:
        f = foes[0]
        if abs(f["feet"][0] - sx) <= 19:
            min_x = min(min_x, f["feet"][0] - 2.2); max_x = max(max_x, f["feet"][0] + 2.2)
            ground = min(ground, f["feet"][1]); top = max(top, f["feet"][1] + 2.1 * f["scale"])
        else:
            max_x = sx + 16.5
    else:
        for p in scene["props"]:
            if p["kind"] in ("Target", "Apple", "Rope") and p["alive"]:
                c = p["shape"]["a"] if p["shape"]["kind"] != "Capsule" else p["shape"]["a"]
                if abs(c[0] - sx) > 26:
                    continue
                min_x = min(min_x, c[0] - 1.2); max_x = max(max_x, c[0] + 1.2); top = max(top, c[1] + 0.8)
    cam = frame(min_x, max_x, ground, top, focus)
    zoom = cam.size / 4.4
    canvas = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(canvas)
    for yy in range(H):
        k = yy / (H - 1)
        d.line((0, yy, W, yy), fill=tuple(int(top_c[i] + (bot_c[i] - top_c[i]) * k) for i in range(3)) + (255,))

    # Sun / moon.
    sun = Sprite("scenery", "moon" if night else "sun")
    draw_sprite(canvas, cam, sun, cam.cx + cam.hw * 0.63, cam.cy + cam.size * 0.53, (1.25 if night else 1.45) * zoom / sun.width_m)
    cloud = Sprite("scenery", "cloud")
    for i in range(6):
        cxp, cyp, cs = rnd.uniform(-1.2, 1.2), rnd.uniform(0.25, 0.85), rnd.uniform(0.5, 1.05)
        draw_sprite(canvas, cam, cloud, cam.cx + cxp * cam.hw, cam.cy - cam.size + cyp * cam.size * 2, 2.6 * cs * zoom / cloud.width_m,
                    color=(0.8, 0.8, 1, 0.16) if night else (1, 1, 1, 0.92))
    # Parallax.
    mid = tuple((far[i] + world[i]) / 2 for i in range(3))
    near = tuple(far[i] + (world[i] - far[i]) * 0.8 for i in range(3))
    for name, follow, scale, lift, col in (("hills_far", 0.92, 0.62, -0.3, far), ("forest_far", 0.84, 0.66, -0.55, mid),
                                           ("hills_near", 0.7, 0.6, -1.05, near)):
        spr = Sprite("backdrop", name)
        s = scale * zoom
        tile_w = spr.width_m * s
        bottom = cam.cy - cam.size + lift * zoom
        y = bottom - spr.min_y_m * s
        base = cam.cx * follow
        start = base + math.floor((cam.cx - base - cam.hw) / tile_w) * tile_w
        for k in range(5):
            draw_sprite(canvas, cam, spr, start + (k + 0.5) * tile_w, y, s, color=col)

    world_c = world
    busy = [f["feet"][0] for f in fighters] + [p["shape"]["a"][0] for p in scene["props"]]
    # Islands.
    for gi, g in enumerate(scene["grounds"]):
        gx, gy, hx, hy = g
        topy, left, right = gy + hy, gx - hx, gx + hx
        width = right - left
        capw = 0.55
        midspr = Sprite("props", "island_mid")
        inner = max(0.2, width - 2 * capw)
        n = max(1, round(inner))
        tw = inner / n
        for i in range(n):
            put(-200, lambda c, x=left + capw + (i + 0.5) * tw, t=topy, tw=tw: draw_sprite(c, cam, midspr, x, t, tw / midspr.width_m * 1.01, 1.0, color=world_c))
        put(-201, lambda c, x=left + capw + 0.02, t=topy: draw_sprite(c, cam, Sprite("props", "island_left"), x, t, color=world_c))
        put(-201, lambda c, x=right - capw - 0.02, t=topy: draw_sprite(c, cam, Sprite("props", "island_right"), x, t, color=world_c))
        for i, (nm, share, at) in enumerate((("island_under_1", 0.78, 0.5), ("island_under_2", 0.46, 0.22), ("island_under_3", 0.36, 0.8))):
            u = Sprite("props", nm)
            s = min(1.7, width * share / u.width_m)
            put(-210 - i, lambda c, u=u, s=s, x=left + (right - left) * at, t=topy: draw_sprite(c, cam, u, x, t - 1.2 + 0.12, s, color=world_c))
        if width >= 4:
            for i in range(2 if width >= 7 else 1):
                x = left + rnd.uniform(0.5, 1.1) if i == 0 else right - rnd.uniform(0.5, 1.1)
                tree = Sprite("scenery", "tree_night" if night else rnd.choice(["tree_pine", "tree_round"]))
                hgt = rnd.uniform(2.5, 3.3)
                put(-300, lambda c, t=tree, x=x, hgt=hgt, ty=topy: draw_sprite(c, cam, t, x, ty + 0.05, hgt / t.height_m, color=world_c))
        for i in range(max(1, min(4, round(width / 2.6)))):
            b = Sprite("scenery", rnd.choice(["bush", "bush_berry"]))
            x = rnd.uniform(left + 0.4, right - 0.4)
            wdt = rnd.uniform(0.9, 1.3)
            put(-150, lambda c, b=b, x=x, wdt=wdt, ty=topy: draw_sprite(c, cam, b, x, ty + 0.04, wdt / b.width_m, color=world_c))
        x = left + 0.25
        while x < right - 0.2:
            if all(abs(bx - x) >= 0.55 for bx in busy):
                pick = rnd.random()
                if pick < 0.55:
                    s = Sprite("scenery", "grass"); sc = rnd.uniform(0.32, 0.46) / s.width_m
                elif pick < 0.82:
                    s = Sprite("scenery", rnd.choice(["flower_pink", "flower_white", "flower_yellow"])); sc = rnd.uniform(0.34, 0.46) / s.height_m
                elif pick < 0.92:
                    s = Sprite("scenery", "mushroom"); sc = rnd.uniform(0.28, 0.38) / s.height_m
                else:
                    s = Sprite("scenery", "rock"); sc = rnd.uniform(0.4, 0.6) / s.width_m
                put(700, lambda c, s=s, x=x, sc=sc, ty=topy: draw_sprite(c, cam, s, x, ty + 0.02, sc, color=world_c))
            x += rnd.uniform(0.55, 0.95)

    for w in scene["walls"]:
        put(38, lambda c, w=w: draw_sliced(c, cam, Sprite("props", "stone_wall"), w[0], w[1], w[2] * 2 + 0.06, w[3] * 2 + 0.08, color=world_c))

    for i, p in enumerate(scene["props"]):
        if not p["alive"]:
            continue
        draw_prop(p, cam, world_c, scene, 40 + i * 4)

    for i, f in enumerate(fighters):
        if not f["active"]:
            continue
        fx, fy = f["feet"]
        pose = Sprite("Poses", f["id"] + "_idle") if f["id"] + "_idle" in atlas("Poses")[1] else Sprite("Poses", "bandit_idle")
        sc = 2.0 * f["scale"] / pose.height_m
        shadow = Sprite("fx", "shadow")
        put(199 + i * 50, lambda c, fx=fx, fy=fy, s=f["scale"]: draw_sprite(c, cam, shadow, fx, fy, 1.1 * s / shadow.width_m, color=(0, 0, 0, 0.35)))
        # Pose pictures have their pivot in the middle: stand them on their feet.
        put(200 + i * 50, lambda c, pose=pose, fx=fx, fy=fy, sc=sc, fl=f["facing"] < 0: draw_sprite(c, cam, pose, fx, fy - pose.min_y_m * sc, sc, flip=fl, color=world_c))

    for order, _, fn in sorted(LAYERS, key=lambda t: (t[0], t[1])):
        fn(canvas)
    # Ground line guide (design: y 316 of 390) for the check.
    gy = cam.px(0, ground)[1]
    ImageDraw.Draw(canvas).line((0, gy, 40, gy), fill=(255, 0, 0, 255), width=3)
    path = os.path.join(out_dir, scene["name"] + ".png")
    canvas.convert("RGB").save(path)
    return path


def ground_below(scene, x, y):
    best = -99
    for g in scene["grounds"]:
        topy = g[1] + g[3]
        if abs(x - g[0]) <= g[2] and topy <= y + 0.01 and topy > best:
            best = topy
    return best


def draw_prop(p, cam, col, scene, order):
    k = p["kind"]
    s = p["shape"]
    ax, ay = s["a"]
    hx, hy = s["h"]
    r = s["r"]
    if k == "Wall" and any(q["kind"] == "Rope" and abs(q["shape"]["a"][0] - ax) < 0.3 and abs((q["shape"]["a"][1] - q["shape"]["h"][1]) - (ay + hy)) < 0.35 for q in scene["props"]):
        return  # the Rescue cage: drawn by its rope
    if k == "Wall":
        name = "stone_wall" if hy * 2 > 4 else "wall"
        put(order, lambda c: draw_sliced(c, cam, Sprite("props", name), ax, ay, hx * 2 + 0.06, hy * 2 + 0.08, color=col))
    elif k in ("Crate", "TntCrate"):
        spr = Sprite("props", "crate" if k == "Crate" else "tnt")
        put(order, lambda c: draw_sprite(c, cam, spr, ax, ay, hx * 2 * 1.08 / spr.width_m, color=col))
    elif k == "ExplosiveBarrel":
        spr = Sprite("props", "barrel")
        put(order, lambda c: draw_sprite(c, cam, spr, ax, ay, r * 2.3 / spr.height_m, color=col))
    elif k == "BouncePad":
        spr = Sprite("props", "pad")
        if hy > hx:
            put(order, lambda c: draw_sliced(c, cam, spr, ax, ay, hy * 2 + 0.1, max(0.36, hx * 2 + 0.16), rot=90, color=col))
        else:
            put(order, lambda c: draw_sliced(c, cam, spr, ax, ay, hx * 2 + 0.1, max(0.36, hy * 2 + 0.16), color=col))
    elif k == "Target":
        spr = Sprite("props", "target")
        put(order + 2, lambda c: draw_sprite(c, cam, spr, ax, ay, r * 2.15 / spr.width_m, color=col))
        if p["motion"] == "PingPong" or (p["motion"] == "None" and ground_below(scene, ax, ay - r) <= -50):
            b = Sprite("props", "balloon")
            put(order + 1, lambda c: draw_sprite(c, cam, b, ax, ay + r + 1.05, 0.95 / b.height_m, color=col))
        elif p["motion"] == "Swing":
            px_, py_ = p["pivot"]
            rope = Sprite("props", "rope")
            L = math.hypot(px_ - ax, py_ - ay) - r
            put(order, lambda c: draw_tiled(c, cam, rope, ax, ay + r + L / 2, 0.1, L, color=col))
        else:
            g = ground_below(scene, ax, ay - r)
            L = ay - g
            post = Sprite("props", "target_post")
            piv_y = 1 - post.py / post.h
            put(order, lambda c: draw_sliced(c, cam, post, ax, g + piv_y * L, 0.2, L, color=col))
            st = Sprite("props", "target_stand")
            put(order + 1, lambda c: draw_sprite(c, cam, st, ax, g + 0.04, 0.95 / st.width_m, color=col))
    elif k == "Apple":
        spr = Sprite("props", "apple")
        put(order + 2, lambda c: draw_sprite(c, cam, spr, ax, ay, r * 2.6 / spr.width_m, color=col))
    elif k == "Dummy":
        spr = Sprite("props", "dummy")
        put(order, lambda c: draw_sprite(c, cam, spr, ax, ay - hy, hy * 2 * 1.06 / spr.height_m, color=col))
    elif k == "Rope":
        rope = Sprite("props", "rope")
        put(order, lambda c: draw_tiled(c, cam, rope, ax, ay, 0.16, hy * 2, color=col))
        cage = Sprite("props", "cage")
        walls = [q for q in scene["props"] if q["kind"] == "Wall" and abs(q["shape"]["a"][0] - ax) < 0.3 and abs((ay - hy) - (q["shape"]["a"][1] + q["shape"]["h"][1])) < 0.35]
        cs = (walls[0]["shape"]["h"][1] * 2 * 1.15 if walls else 1.3) / cage.height_m
        if walls:
            wc = walls[0]["shape"]["a"][1]
            center_off = (cage.max_y_m + cage.min_y_m) / 2 * cs
            cy = wc - center_off
        else:
            cy = ay - hy - cage.max_y_m * cs
        put(order + 2, lambda c: draw_sprite(c, cam, cage, ax, cy, cs, color=col))
        fox = Sprite("props", "fox")
        fs = cs * cage.height_m * 0.6 / fox.height_m
        cage_bottom = cy + cage.min_y_m * cs
        put(order + 1, lambda c: draw_sprite(c, cam, fox, ax - 0.03, cage_bottom + 0.1 - fox.min_y_m * fs, fs, color=col))
    elif k == "Shield":
        spr = Sprite("props", "shield_boss" if p["slot"] >= 0 else "shield_tall")
        put(350, lambda c: draw_sprite(c, cam, spr, ax, ay, hy * 2 * 1.08 / spr.height_m, flip=p.get("facing", 1) < 0, color=col))
    elif k == "Platform":
        spr = Sprite("props", "platform")
        put(order, lambda c: draw_sliced(c, cam, spr, ax, ay, hx * 2 + 0.12, max(0.42, hy * 2 + 0.08), color=col))
        pr = Sprite("props", "propeller")
        put(order - 1, lambda c: draw_sprite(c, cam, pr, ax, ay - hy - 0.2, 0.9 / pr.width_m, color=col))


def main():
    scenes = json.load(open(sys.argv[1]))
    out = sys.argv[2] if len(sys.argv) > 2 else "."
    os.makedirs(out, exist_ok=True)
    for s in scenes:
        print(render(s, out))


if __name__ == "__main__":
    main()
