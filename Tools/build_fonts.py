#!/usr/bin/env python3
"""
Builds the game's fonts (CLAUDE.md: Tools/build_fonts.py).

- Fredoka (display) and Nunito (body), both SIL OFL, from google/fonts: the variable fonts are cut into the static
  weights the design uses and subset to Latin (English + Hinglish are Latin script).
- Material Symbols Rounded (Apache 2.0) icons: filled, weight 600, subset to the icons the game uses, plus the
  generated Assets/ArcherArcade/Runtime/UI/Kit/Icons.cs with one constant per icon.

Output: Assets/ArcherArcade/Resources/Fonts/*.ttf (+ licence files). Unity imports them as dynamic fonts and the
runtime builds TextMeshPro SDF assets from them (FontLibrary), so no editor step is needed.

Usage: python3 Tools/build_fonts.py      (needs: pip install fonttools brotli)
"""
import os
import sys
import urllib.request

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CACHE = os.path.join(ROOT, "Tools", ".cache", "fonts")
OUT = os.path.join(ROOT, "Assets", "ArcherArcade", "Resources", "Fonts")
ICONS_CS = os.path.join(ROOT, "Assets", "ArcherArcade", "Runtime", "UI", "Kit", "Icons.cs")

RAW = "https://raw.githubusercontent.com"
SOURCES = {
    "Fredoka.ttf": RAW + "/google/fonts/main/ofl/fredoka/Fredoka%5Bwdth,wght%5D.ttf",
    "Nunito.ttf": RAW + "/google/fonts/main/ofl/nunito/Nunito%5Bwght%5D.ttf",
    "OFL-Fredoka.txt": RAW + "/google/fonts/main/ofl/fredoka/OFL.txt",
    "OFL-Nunito.txt": RAW + "/google/fonts/main/ofl/nunito/OFL.txt",
    "MaterialSymbolsRounded.ttf": RAW + "/google/material-design-icons/master/variablefont/"
                                  "MaterialSymbolsRounded%5BFILL,GRAD,opsz,wght%5D.ttf",
    "MaterialSymbolsRounded.codepoints": RAW + "/google/material-design-icons/master/variablefont/"
                                         "MaterialSymbolsRounded%5BFILL,GRAD,opsz,wght%5D.codepoints",
    "LICENSE-MaterialSymbols.txt": RAW + "/google/material-design-icons/master/LICENSE",
}

# (output file, source, axis values)
TEXT_FONTS = [
    ("Fredoka-Bold.ttf", "Fredoka.ttf", {"wght": 700, "wdth": 100}),
    ("Fredoka-SemiBold.ttf", "Fredoka.ttf", {"wght": 600, "wdth": 100}),
    ("Nunito-Bold.ttf", "Nunito.ttf", {"wght": 700}),
    ("Nunito-ExtraBold.ttf", "Nunito.ttf", {"wght": 800}),
    ("Nunito-Black.ttf", "Nunito.ttf", {"wght": 900}),
]

# Basic Latin, Latin-1, Latin Extended-A, general punctuation (– — ‘ ’ “ ” • …), ₹, arrows, ×, ★, ™.
TEXT_UNICODES = "U+0020-007E,U+00A0-00FF,U+0100-017F,U+2010-2027,U+2030-203A,U+20B9,U+2190-2193,U+00D7,U+2605,U+2122"

# Every icon the UI uses (Material Symbols names). Keep sorted; Icons.cs is generated from this list.
ICONS = sorted(set("""
    ac_unit add adjust air arrow_back arrow_forward arrow_right_alt arrow_upward auto_awesome back_hand bar_chart
    battery_saver bedtime bolt bomb brush bubble_chart cached calendar_month call_split card_giftcard celebration
    check check_circle checkroom chevron_left chevron_right close contrast crown dark_mode diamond domain_disabled
    done_all edit electric_bolt emoji_events exit_to_app expand_more explosion favorite fitness_center flag forest
    gesture group groups handshake healing health_and_safety help home hourglass_empty info inventory_2 light_mode
    local_fire_department lock lock_open logout map military_tech monetization_on motion_photos_off music_note
    nightlight nutrition palette pause person play_arrow privacy_tip psychology redeem remove replay rocket_launch
    savings science settings shield shuffle skull speed sports_score star storefront straighten swap_horiz swords
    target thunderstorm timer touch_app track_changes translate trending_up tune upgrade vibration visibility
    volume_off volume_up water_drop wb_sunny wb_twilight weight workspace_premium zoom_in
""".split()))


def fetch():
    os.makedirs(CACHE, exist_ok=True)
    for name, url in SOURCES.items():
        path = os.path.join(CACHE, name)
        if not os.path.exists(path):
            print("download", name)
            urllib.request.urlretrieve(url, path)


def build_text_fonts():
    for out, src, axes in TEXT_FONTS:
        font = TTFont(os.path.join(CACHE, src))
        static = instancer.instantiateVariableFont(font, axes)
        opts = subset.Options()
        opts.layout_features = ["*"]
        opts.name_IDs = ["*"]
        opts.notdef_outline = True
        s = subset.Subsetter(opts)
        s.populate(unicodes=subset.parse_unicodes(TEXT_UNICODES))
        s.subset(static)
        static.save(os.path.join(OUT, out))
        print("font", out, os.path.getsize(os.path.join(OUT, out)) // 1024, "KB")


def read_codepoints():
    cps = {}
    with open(os.path.join(CACHE, "MaterialSymbolsRounded.codepoints")) as f:
        for line in f:
            parts = line.split()
            if len(parts) == 2:
                cps[parts[0]] = int(parts[1], 16)
    return cps


def build_icons():
    cps = read_codepoints()
    missing = [n for n in ICONS if n not in cps]
    if missing:
        sys.exit("unknown icons: " + ", ".join(missing))
    font = TTFont(os.path.join(CACHE, "MaterialSymbolsRounded.ttf"))
    static = instancer.instantiateVariableFont(font, {"FILL": 1, "wght": 600, "GRAD": 0, "opsz": 24})
    opts = subset.Options()
    opts.layout_features = []
    opts.notdef_outline = True
    s = subset.Subsetter(opts)
    s.populate(unicodes=[cps[n] for n in ICONS])
    s.subset(static)
    out = os.path.join(OUT, "Icons-Rounded.ttf")
    static.save(out)
    print("icons", len(ICONS), os.path.getsize(out) // 1024, "KB")
    write_icons_cs(cps)


def pascal(name):
    return "".join(p[:1].upper() + p[1:] for p in name.split("_"))


def write_icons_cs(cps):
    lines = [
        "// <auto-generated> by Tools/build_fonts.py — do not edit; add icons to ICONS there and re-run.",
        "using System.Collections.Generic;",
        "",
        "namespace ArcherArcade.UI",
        "{",
        "    /// <summary>Material Symbols Rounded glyphs used by the game (font: Resources/Fonts/Icons-Rounded).</summary>",
        "    public static class Icons",
        "    {",
    ]
    for n in ICONS:
        lines.append('        public const string {0} = "\\u{1:04x}";'.format(pascal(n), cps[n]))
    lines += [
        "",
        "        static readonly Dictionary<string, string> ByNameMap = new Dictionary<string, string>",
        "        {",
    ]
    for n in ICONS:
        lines.append('            {{ "{0}", {1} }},'.format(n, pascal(n)))
    lines += [
        "        };",
        "",
        "        /// <summary>Glyph for a Material Symbols name (e.g. from badge data); a star if unknown.</summary>",
        "        public static string ByName(string name) => name != null && ByNameMap.TryGetValue(name, out string g) ? g : Star;",
        "    }",
        "}",
        "",
    ]
    os.makedirs(os.path.dirname(ICONS_CS), exist_ok=True)
    with open(ICONS_CS, "w", newline="\n") as f:
        f.write("\n".join(lines))
    print("wrote", os.path.relpath(ICONS_CS, ROOT))


def copy_licences():
    for name in ("OFL-Fredoka.txt", "OFL-Nunito.txt", "LICENSE-MaterialSymbols.txt"):
        with open(os.path.join(CACHE, name), "rb") as src, open(os.path.join(OUT, name), "wb") as dst:
            dst.write(src.read())


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    fetch()
    build_text_fonts()
    build_icons()
    copy_licences()
