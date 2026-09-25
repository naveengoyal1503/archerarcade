#!/usr/bin/env python3
"""
Archer Arcade store + launcher art from the game's own exported art (original, no third-party assets):

  Assets/ArcherArcade/Art/Icon/icon_1024.png           legacy / Play Store icon (512 is scaled by the store)
  Assets/ArcherArcade/Art/Icon/icon_background.png     adaptive icon background (purple glow)
  Assets/ArcherArcade/Art/Icon/icon_foreground.png     adaptive icon foreground (gold tile + bow, safe zone)
  Docs/Store/icon_512.png                              Play Console icon
  Docs/Store/feature_graphic_1024x500.png              Play Console feature graphic

Look: the splash logo (gold tile #FFD23F with a #D4A514 edge, the bow logo) on the result-screen purple glow
(#8E6BFF → #5536D6), two heroes from the roster facing off, Fredoka title with the design's hard shadow.
Run after Tools/art/build_art.py (it reads Resources/Art atlases): python Tools/art/make_store_art.py
"""
import json
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ART = os.path.join(ROOT, "Assets", "ArcherArcade", "Resources", "Art")
FONTS = os.path.join(ROOT, "Assets", "ArcherArcade", "Resources", "Fonts")
ICON_DIR = os.path.join(ROOT, "Assets", "ArcherArcade", "Art", "Icon")
STORE_DIR = os.path.join(ROOT, "Docs", "Store")

PURPLE_IN = (0x8E, 0x6B, 0xFF)
PURPLE_OUT = (0x55, 0x36, 0xD6)
GOLD = (0xFF, 0xD2, 0x3F)
GOLD_EDGE = (0xD4, 0xA5, 0x14)
NAVY = (0x2A, 0x23, 0x50)
SHADOW = (0x3A, 0x1F, 0xB0)


def sprite(atlas_png, atlas_json, name):
    img = Image.open(atlas_png).convert("RGBA")
    d = json.load(open(atlas_json))["sprites"][name]
    x, y, w, h = int(d["x"]), int(d["y"]), int(d["w"]), int(d["h"])
    return img.crop((x, y, x + w, y + h))


def radial(size, inner, outer, cx=0.5, cy=0.4, reach=0.75):
    w, h = size
    img = Image.new("RGB", size)
    px = img.load()
    diag = max(w, h) * reach
    for y in range(h):
        for x in range(w):
            d = ((x - cx * w) ** 2 + (y - cy * h) ** 2) ** 0.5 / diag
            t = min(1.0, d)
            px[x, y] = tuple(int(inner[i] + (outer[i] - inner[i]) * t) for i in range(3))
    return img


def rounded(size, radius, color):
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(img).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius, fill=color)
    return img


def logo_tile(size):
    """Gold tile with its edge and the bow (splash logo), `size` px square plus the edge below."""
    edge = int(size * 7 / 96)
    tile = Image.new("RGBA", (size, size + edge), (0, 0, 0, 0))
    r = int(size * 30 / 96)
    tile.alpha_composite(rounded((size, size), r, GOLD_EDGE + (255,)), (0, edge))
    tile.alpha_composite(rounded((size, size), r, GOLD + (255,)), (0, 0))
    # soft top highlight
    hl = rounded((int(size * 0.8), int(size * 0.3)), int(r * 0.7), (255, 255, 255, 60))
    tile.alpha_composite(hl, (int(size * 0.1), int(size * 0.06)))
    bow = sprite(os.path.join(ART, "Sprites", "ui.png"), os.path.join(ART, "Sprites", "ui.json"), "logo_bow")
    scale = (size * 0.76) / max(bow.size)
    bow = bow.resize((int(bow.size[0] * scale), int(bow.size[1] * scale)), Image.LANCZOS)
    tile.alpha_composite(bow, ((size - bow.size[0]) // 2, (size - bow.size[1]) // 2))
    return tile


def drop_shadow(img, offset, blur, alpha):
    a = img.split()[-1].point(lambda v: int(v * alpha))
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sh.putalpha(a)
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    out = Image.new("RGBA", (img.size[0] + abs(offset[0]) + blur * 4, img.size[1] + abs(offset[1]) + blur * 4), (0, 0, 0, 0))
    base = (blur * 2, blur * 2)
    out.alpha_composite(sh, (base[0] + offset[0], base[1] + offset[1]))
    out.alpha_composite(img, base)
    return out


def icon():
    os.makedirs(ICON_DIR, exist_ok=True)
    os.makedirs(STORE_DIR, exist_ok=True)
    bg = radial((1024, 1024), PURPLE_IN, PURPLE_OUT, 0.5, 0.42, 0.8).convert("RGBA")
    bg.save(os.path.join(ICON_DIR, "icon_background.png"))
    # Adaptive foreground: art inside the 66 % safe zone.
    fg = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    tile = drop_shadow(logo_tile(560), (0, 18), 16, 0.35)
    fg.alpha_composite(tile, ((1024 - tile.size[0]) // 2, (1024 - tile.size[1]) // 2 + 8))
    fg.save(os.path.join(ICON_DIR, "icon_foreground.png"))
    full = bg.copy()
    big = drop_shadow(logo_tile(700), (0, 22), 20, 0.35)
    full.alpha_composite(big, ((1024 - big.size[0]) // 2, (1024 - big.size[1]) // 2 + 10))
    full.convert("RGB").save(os.path.join(ICON_DIR, "icon_1024.png"))
    full.convert("RGB").resize((512, 512), Image.LANCZOS).save(os.path.join(STORE_DIR, "icon_512.png"))


def feature():
    W, H = 1024, 500
    img = radial((W, H), PURPLE_IN, PURPLE_OUT, 0.5, 0.35, 0.75).convert("RGBA")
    draw = ImageDraw.Draw(img)
    # Ground strip of the forest.
    hills = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(hills)
    hd.ellipse((-200, 330, 560, 700), fill=(0x7E, 0xD9, 0x57, 255))
    hd.ellipse((460, 340, 1240, 720), fill=(0x5F, 0xBF, 0x3F, 255))
    img.alpha_composite(hills)
    # Heroes facing off.
    poses = os.path.join(ART, "Poses.png"), os.path.join(ART, "Poses.json")
    left = sprite(*poses, "ranger_victory")
    right = sprite(*poses, "fire_idle").transpose(Image.FLIP_LEFT_RIGHT)
    for pic, x, anchor in ((left, 36, "l"), (right, W - 36, "r")):
        s = 300 / pic.size[1]
        pic = pic.resize((int(pic.size[0] * s), int(pic.size[1] * s)), Image.LANCZOS)
        pic = drop_shadow(pic, (0, 10), 10, 0.25)
        px = x if anchor == "l" else x - pic.size[0]
        img.alpha_composite(pic, (px, H - pic.size[1] + 8))
    # Logo + title.
    tile = drop_shadow(logo_tile(120), (0, 8), 8, 0.3)
    img.alpha_composite(tile, ((W - tile.size[0]) // 2, 40))
    title_font = ImageFont.truetype(os.path.join(FONTS, "Fredoka-Bold.ttf"), 92)
    sub_font = ImageFont.truetype(os.path.join(FONTS, "Nunito-ExtraBold.ttf"), 26)
    title = "Archer Arcade"
    tw = draw.textlength(title, font=title_font)
    tx, ty = (W - tw) / 2, 190
    draw.text((tx, ty + 7), title, font=title_font, fill=SHADOW)
    draw.text((tx, ty), title, font=title_font, fill=(255, 255, 255))
    sub = "Pull. Aim. Headshot!"
    sw = draw.textlength(sub, font=sub_font)
    pill = rounded((int(sw + 48), 50), 25, GOLD + (255,))
    img.alpha_composite(pill, (int((W - sw - 48) / 2), 305))
    draw.text(((W - sw) / 2, 312), sub, font=sub_font, fill=NAVY)
    img.convert("RGB").save(os.path.join(STORE_DIR, "feature_graphic_1024x500.png"))


if __name__ == "__main__":
    icon()
    feature()
    print("icon + feature graphic written")
