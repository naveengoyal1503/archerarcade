#!/usr/bin/env python3
"""
Composes a character from its exported atlas with poses solved by the game's C# RigSolver (Tools/RigPreview),
next to the design's own renders, so the port can be checked by eye.

Usage: python3 Tools/art/preview_rig.py ranger out.png
"""
import json
import math
import os
import subprocess
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ART = os.path.join(ROOT, "Assets", "ArcherArcade", "Resources", "Art", "Characters")
RAW = os.path.join(ROOT, "Tools", ".cache", "art", "raw")

SPRITE = {
    "UpperArmBack": "upper_arm", "ForearmBack": "forearm", "HandBack": "hand", "UpperLegBack": "upper_leg",
    "LowerLegBack": "lower_leg", "FootBack": "foot", "Cape": "cape", "Quiver": "quiver", "Torso": "torso",
    "UpperLegFront": "upper_leg", "LowerLegFront": "lower_leg", "FootFront": "foot", "Head": "head", "Gear": "gear",
    "Bow": "bow", "UpperArmFront": "upper_arm", "ForearmFront": "forearm", "HandFront": "hand",
}
BACK_ARM = ["UpperArmBack", "ForearmBack", "HandBack"]
ORDER_TAIL = ["UpperLegBack", "LowerLegBack", "FootBack", "Cape", "Quiver", "Torso", "UpperLegFront", "LowerLegFront",
              "FootFront"]


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def compose(rig, atlas, pose, size=420):
    s = rig["pxPerMeter"] / rig["unitsPerMeter"] * rig["scale"]
    canvas = Image.new("RGBA", (size, size), (248, 247, 243, 255))
    gx, gy = size * 0.45, size * 0.9
    lift = pose["lift"]

    def to_px(x, y):
        return gx + x * s, gy + (y - lift) * s

    def paste(name, part):
        key = SPRITE[part]
        if key == "head":
            key = "head_" + pose["expr"]
        meta = rig["parts"].get(key)
        if meta is None:
            return
        img = atlas.crop((meta["x"], meta["y"], meta["x"] + meta["w"], meta["y"] + meta["h"]))
        x, y, rot = pose["parts"][part]
        # rotate around the pivot: pad so the pivot is the centre
        pvx, pvy = meta["px"], meta["py"]
        w, h = img.size
        half = int(math.ceil(max(pvx, w - pvx, pvy, h - pvy))) + 2
        big = Image.new("RGBA", (half * 2, half * 2), (0, 0, 0, 0))
        big.paste(img, (int(round(half - pvx)), int(round(half - pvy))))
        big = big.rotate(-rot, resample=Image.BICUBIC)
        cx, cy = to_px(x, y)
        canvas.alpha_composite(big, (int(round(cx - half)), int(round(cy - half))))

    order = [] if pose["backFront"] else list(BACK_ARM)
    order += ORDER_TAIL
    if pose["backFront"]:
        order += BACK_ARM
    order += ["Head", "Gear", "Bow"]
    for part in order:
        if part == "Cape" and not rig["hasCape"]:
            continue
        paste(SPRITE[part], part)
    # string
    t = pose["string"]
    ink = rig["pxPerMeter"] * 0.0404 * rig["scale"] / rig["scale"]
    d = ImageDraw.Draw(canvas)
    pts = [to_px(t[0], t[1]), to_px(t[2], t[3]), to_px(t[4], t[5])]
    if rig["bowStyle"] != "crossbow":
        d.line(pts, fill=(0x2A, 0x1B, 0x30, 255), width=max(2, int(ink)))
        d.line(pts, fill=hex_rgb(rig["stringColor"]) + (255,), width=max(1, int(ink * 0.43)))
    ax, ay, aa, vis = pose["arrow"]
    if vis:
        L = 122
        sx, sy = to_px(ax, ay)
        ex, ey = sx + math.cos(math.radians(aa)) * L * s, sy + math.sin(math.radians(aa)) * L * s
        d.line([(sx, sy), (ex, ey)], fill=(0x2A, 0x1B, 0x30, 255), width=6)
        d.line([(sx, sy), (ex, ey)], fill=(0xD9, 0xB2, 0x7A, 255), width=3)
    for part in ["UpperArmFront", "ForearmFront", "HandFront"]:
        paste(SPRITE[part], part)
    d = ImageDraw.Draw(canvas)
    d.line([(0, gy), (size, gy)], fill=(214, 208, 196, 255), width=2)
    return canvas


def main():
    look = sys.argv[1] if len(sys.argv) > 1 else "ranger"
    out = sys.argv[2] if len(sys.argv) > 2 else "rig_preview.png"
    rig = json.load(open(os.path.join(ART, look + ".json")))
    atlas = Image.open(os.path.join(ART, look + ".png")).convert("RGBA")
    args = ["crossbow" if rig["bowStyle"] == "crossbow" else "bow"]
    names = {"TW": "TW", "TH": "TH", "UA": "UA", "FA": "FA", "UL": "UL", "LL": "LL", "wUA": "WUA", "wFA": "WFA",
             "wUL": "WUL", "wLL": "WLL", "hr": "HR", "bH": "BH", "bD": "BD", "k": "K"}
    for k, v in names.items():
        args += [v, str(rig["dims"][k])]
    env = dict(os.environ, PATH=os.environ.get("PATH", "") + ":/root/.dotnet", DOTNET_ROOT="/root/.dotnet")
    res = subprocess.run(["dotnet", "run", "--no-build", "--project", os.path.join(ROOT, "Tools", "RigPreview"), "--"] + args,
                         capture_output=True, text=True, env=env, check=True)
    poses = json.loads(res.stdout)
    tiles = [compose(rig, atlas, p) for p in poses.values()]
    names = list(poses.keys())
    design = []
    for n in ["idle", "drawing", "full_draw", "release", "hit", "victory"]:
        path = os.path.join(RAW, "poses", look + "_" + n + ".png")
        if os.path.exists(path):
            im = Image.open(path).convert("RGBA")
            bg = Image.new("RGBA", im.size, (235, 240, 250, 255))
            bg.alpha_composite(im)
            design.append(bg.resize((420, 420)))
    cols = 6
    rows = (len(tiles) + cols - 1) // cols + 1
    sheet = Image.new("RGBA", (cols * 420, rows * 440), (255, 255, 255, 255))
    for i, t in enumerate(design):
        sheet.alpha_composite(t, (i * 420, 0))
    for i, t in enumerate(tiles):
        r, c = divmod(i, cols)
        sheet.alpha_composite(t, (c * 420, (r + 1) * 440))
        ImageDraw.Draw(sheet).text((c * 420 + 8, (r + 1) * 440 + 4), names[i], fill=(0, 0, 0, 255))
    sheet.save(out)
    print(out)


if __name__ == "__main__":
    main()
