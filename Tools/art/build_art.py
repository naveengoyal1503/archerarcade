#!/usr/bin/env python3
"""
Archer Arcade art build, step 2 of 2 (step 1: node Tools/art/export_art.mjs).

Takes the raw renders in Tools/.cache/art/raw/, adds the 2.5D look and packs atlases for Unity:

- 2.5D shading: every sprite is "inflated" into a soft 3D form (a height field from the distance to the silhouette,
  plus a small pillow per colour region between the ink lines), lit by a key light from the upper right (the side the
  design already highlights), with a soft specular glint. The design's flat colours, shade shapes and ink lines stay;
  the form reads like a vinyl toy instead of a flat sticker.
- Trimmed parts are shelf-packed into one atlas per character look (+ a JSON rig file with rects and pivots), and
  props / effects / UI pictures into group atlases.

Output: Assets/ArcherArcade/Resources/Art/{Characters,Portraits,Poses,Sprites}/ (*.png + *.json).
Usage: python3 Tools/art/build_art.py      (needs: pip install pillow numpy scipy)
"""
import json
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RAW = os.path.join(ROOT, "Tools", ".cache", "art", "raw")
OUT = os.path.join(ROOT, "Assets", "ArcherArcade", "Resources", "Art")

INK = np.array([0x2A, 0x1B, 0x30], dtype=np.float32)
LIGHT = np.array([0.42, -0.58, 0.70], dtype=np.float32)  # x right, y down (image space), z towards the viewer
LIGHT /= np.linalg.norm(LIGHT)
HALF = LIGHT + np.array([0, 0, 1], dtype=np.float32)
HALF /= np.linalg.norm(HALF)
PAD = 2


def dome(dist, radius):
    """Circular profile: 0 at the edge, `radius` in the middle (a sphere / cylinder cross-section)."""
    t = np.clip(dist / max(radius, 1e-3), 0, 1)
    return radius * np.sqrt(1 - (1 - t) ** 2)


def shade(img, strength=1.0):
    """Adds the 2.5D volume shading to an RGBA image (uint8 array), returns a new array."""
    if strength <= 0:
        return img
    rgba = img.astype(np.float32) / 255.0
    a = rgba[..., 3]
    inside = a > 0.5
    if not inside.any():
        return img
    rgb = rgba[..., :3]
    # Silhouette dome.
    d1 = ndimage.distance_transform_edt(inside)
    r1 = min(float(d1.max()), 90.0)
    # Smooth the medial-axis ridge of the distance field so big shapes read as round, not faceted.
    h = ndimage.gaussian_filter(0.85 * dome(d1, r1), max(1.2, r1 * 0.12))
    # Per-region pillow: distance to the ink lines (dark outline colour) or the outside.
    ink = (np.abs(rgb * 255 - INK).sum(axis=2) < 60) & inside
    d2 = ndimage.distance_transform_edt(inside & ~ink)
    h += ndimage.gaussian_filter(0.45 * dome(d2, min(12.0, max(3.0, r1 * 0.35))), 1.2)
    gy, gx = np.gradient(h)
    n = np.dstack([-gx * 0.9, -gy * 0.9, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    ndl = np.clip((n * LIGHT).sum(axis=2), 0, 1)
    flat = float(LIGHT[2])  # ndl of a flat, front-facing surface
    diffuse = 1.0 + 0.55 * (ndl - flat)                 # brighter towards the light, darker away
    spec = np.clip((n * HALF).sum(axis=2), 0, 1) ** 48 * 0.30
    rim = np.clip(1 - n[..., 2], 0, 1) ** 2 * 0.10      # thin bright rim on steep edges
    f = 1 + (diffuse - 1) * strength
    out = rgb * f[..., None] + ((spec + rim) * strength)[..., None]
    out = np.where(ink[..., None], rgb, out)            # ink lines stay ink
    out = np.clip(out, 0, 1)
    res = np.dstack([out, a[..., None]])
    return (res * 255 + 0.5).astype(np.uint8)


def trim(img):
    """Crops to the non-transparent pixels plus PAD; returns (image, left, top)."""
    a = img[..., 3]
    ys, xs = np.nonzero(a > 2)
    if len(xs) == 0:
        return img[:1, :1], 0, 0
    x0, x1, y0, y1 = int(xs.min()), int(xs.max()) + 1, int(ys.min()), int(ys.max()) + 1
    h, w = a.shape
    if x0 == 0 or y0 == 0 or x1 == w or y1 == h:
        print("warning: sprite touches its render edge (clipped?)", file=sys.stderr)
    x0, y0 = max(0, x0 - PAD), max(0, y0 - PAD)
    x1, y1 = min(w, x1 + PAD), min(h, y1 + PAD)
    return img[y0:y1, x0:x1], x0, y0


def bleed(img):
    """Copies edge colours into the transparent border so bilinear filtering never shows a dark fringe."""
    a = img[..., 3] > 0
    if a.all() or not a.any():
        return img
    _, (iy, ix) = ndimage.distance_transform_edt(~a, return_indices=True)
    out = img.copy()
    out[..., :3] = img[iy, ix, :3]
    out[..., 3] = img[..., 3]
    return out


class Packer:
    """Simple shelf packer; grows the atlas height as needed (width fixed)."""

    def __init__(self, width):
        self.width = width
        self.items = []

    def add(self, key, img):
        self.items.append((key, img))

    def pack(self):
        order = sorted(self.items, key=lambda kv: -kv[1].shape[0])
        x = y = shelf = 0
        rects = {}
        for key, img in order:
            h, w = img.shape[:2]
            if w + 2 > self.width:
                raise ValueError("sprite wider than atlas: " + key)
            if x + w + 2 > self.width:
                x, y, shelf = 0, y + shelf + 2, 0
            rects[key] = (int(x + 1), int(y + 1), int(w), int(h))
            x += w + 2
            shelf = max(shelf, h)
        height = y + shelf + 2
        pot = 64
        while pot < height:
            pot *= 2
        atlas = np.zeros((pot, self.width, 4), dtype=np.uint8)
        for key, img in self.items:
            x0, y0, w, h = rects[key]
            atlas[y0:y0 + h, x0:x0 + w] = img
        return atlas, rects


def load(rel):
    return np.array(Image.open(os.path.join(RAW, rel)).convert("RGBA"))


def save_png(arr, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(arr, "RGBA").save(path, optimize=True)


def atlas_width(images):
    area = sum(i.shape[0] * i.shape[1] for i in images) * 1.25
    widest = max(i.shape[1] for i in images) + 2
    w = 256
    while w < widest or w * w < area:
        w *= 2
    return min(w, 2048) if widest <= 2048 else widest


def resize(img, max_side):
    h, w = img.shape[:2]
    s = max_side / max(h, w)
    if s >= 1:
        return img
    im = Image.fromarray(img, "RGBA").resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
    return np.array(im)


def build_characters(manifest):
    out_dir = os.path.join(OUT, "Characters")
    for look in manifest["looks"]:
        parts = {}
        for name, p in look["parts"].items():
            img = shade(load(p["file"]))
            img, left, top = trim(img)
            parts[name] = (bleed(img), p["originX"] - left, p["originY"] - top)
        width = atlas_width([v[0] for v in parts.values()])
        packer = Packer(width)
        for name, (img, _, _) in parts.items():
            packer.add(name, img)
        atlas, rects = packer.pack()
        save_png(atlas, os.path.join(out_dir, look["id"] + ".png"))
        rig = {k: look[k] for k in ("id", "name", "hero", "scale", "gear", "hasCape", "boss", "bowStyle", "stringColor",
                                    "stringGlow", "swatches", "weakSpot", "dims")}
        rig["unitsPerMeter"] = manifest["unitsPerMeter"]
        rig["pxPerMeter"] = manifest["pxPerMeter"]
        rig["atlasWidth"], rig["atlasHeight"] = int(atlas.shape[1]), int(atlas.shape[0])
        rig["parts"] = {}
        for name, (x, y, w, h) in rects.items():
            _, px, py = parts[name]
            rig["parts"][name] = {"x": x, "y": y, "w": w, "h": h, "px": round(float(px), 2), "py": round(float(py), 2)}
        with open(os.path.join(out_dir, look["id"] + ".json"), "w") as f:
            json.dump(rig, f, indent=1, sort_keys=True)
        print("character", look["id"], atlas.shape[1], "x", atlas.shape[0])


def build_pictures(manifest):
    """Portraits (per look strip) and whole-body idle / victory pictures for UI."""
    port = Packer(2048)
    poses = Packer(2048)
    for look in manifest["looks"]:
        for ex, rel in look["portraits"].items():
            port.add(look["id"] + "_" + ex, bleed(resize(shade(load(rel), 0.8), 160)))
        for pose in ("idle", "victory"):
            img, _, _ = trim(shade(load(look["poses"][pose]), 0.8))
            poses.add(look["id"] + "_" + pose, bleed(resize(img, 300)))
    for name, packer in (("Portraits", port), ("Poses", poses)):
        atlas, rects = packer.pack()
        save_png(atlas, os.path.join(OUT, name + ".png"))
        with open(os.path.join(OUT, name + ".json"), "w") as f:
            json.dump({"atlasWidth": int(atlas.shape[1]), "atlasHeight": int(atlas.shape[0]),
                       "sprites": {k: {"x": x, "y": y, "w": w, "h": h, "px": w / 2, "py": h / 2}
                                   for k, (x, y, w, h) in rects.items()}}, f, indent=1, sort_keys=True)
        print(name.lower(), atlas.shape[1], "x", atlas.shape[0])


def build_sprites(manifest):
    groups = {}
    for s in manifest["sprites"]:
        groups.setdefault(s["group"], []).append(s)
    for group, items in groups.items():
        imgs = {}
        for s in items:
            img = shade(load(s["file"]), s["shade"])
            if s["trim"]:
                img, left, top = trim(img)
            else:
                left = top = 0
            imgs[s["id"]] = (bleed(img), s["originX"] - left, s["originY"] - top, s)
        packer = Packer(atlas_width([v[0] for v in imgs.values()]))
        for k, v in imgs.items():
            packer.add(k, v[0])
        atlas, rects = packer.pack()
        save_png(atlas, os.path.join(OUT, "Sprites", group + ".png"))
        meta = {"atlasWidth": int(atlas.shape[1]), "atlasHeight": int(atlas.shape[0]), "sprites": {}}
        for k, (x, y, w, h) in rects.items():
            _, px, py, s = imgs[k]
            e = {"x": x, "y": y, "w": w, "h": h, "px": round(float(px), 2), "py": round(float(py), 2), "ppm": s["pxPerMeter"]}
            if s.get("border"):
                e["border"] = s["border"]
            meta["sprites"][k] = e
        with open(os.path.join(OUT, "Sprites", group + ".json"), "w") as f:
            json.dump(meta, f, indent=1, sort_keys=True)
        print("sprites", group, len(items), atlas.shape[1], "x", atlas.shape[0])


def main():
    with open(os.path.join(RAW, "manifest.json")) as f:
        manifest = json.load(f)
    build_characters(manifest)
    build_pictures(manifest)
    if manifest["sprites"]:
        build_sprites(manifest)


if __name__ == "__main__":
    main()
