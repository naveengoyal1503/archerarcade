using UnityEngine;

namespace ArcherArcade.Theme
{
    /// <summary>
    /// Theme-dependent colors from Docs/DESIGN_TOKENS.md §1–2 (prototype Component.LIGHT / DARK).
    /// Values here must match the tokens file exactly; change the doc first, then this file.
    /// </summary>
    public sealed class Palette
    {
        public Color BgTop;
        public Color BgBottom;
        public Color Card;
        public Color Solid;
        public Color Line;
        public Color Ink;
        public Color InkMuted;
        public Color Track;
        public Color Shadow;
        public Color Hill;

        public static readonly Palette Light = new Palette
        {
            BgTop = Hex(0xBFE6FF),
            BgBottom = Hex(0xFFF0DA),
            Card = Hex(0xFFFFFF, 0.72f),
            Solid = Hex(0xFFFFFF),
            Line = Hex(0xFFFFFF, 0.95f),
            Ink = Hex(0x2A2350),
            InkMuted = Hex(0x5F5985),
            Track = Hex(0x2A2350, 0.12f),
            Shadow = Hex(0x2A2350, 0.18f),
            Hill = Hex(0x8BD346)
        };

        public static readonly Palette Dark = new Palette
        {
            BgTop = Hex(0x1E1A45),
            BgBottom = Hex(0x3A2766),
            Card = Hex(0xFFFFFF, 0.08f),
            Solid = Hex(0x2E2860),
            Line = Hex(0xFFFFFF, 0.14f),
            Ink = Hex(0xFFFFFF),
            InkMuted = Hex(0xC4BEEA),
            Track = Hex(0xFFFFFF, 0.14f),
            Shadow = Hex(0x000000, 0.35f),
            Hill = Hex(0x2F7A4C)
        };

        /// <summary>Converts 0xRRGGBB (sRGB) to a Color.</summary>
        public static Color Hex(uint rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }
    }
}
