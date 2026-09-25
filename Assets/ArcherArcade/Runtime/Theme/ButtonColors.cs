using UnityEngine;

namespace ArcherArcade.Theme
{
    /// <summary>Face / edge / text colors of the 3D buttons (DESIGN_TOKENS §2.1). Same in light and dark.</summary>
    public readonly struct ButtonColors
    {
        public readonly Color Face;
        public readonly Color Edge;
        public readonly Color Text;

        public ButtonColors(Color face, Color edge, Color text)
        {
            Face = face;
            Edge = edge;
            Text = text;
        }

        public static ButtonColors For(ButtonStyle style)
        {
            Color white = Palette.Hex(0xFFFFFF);
            switch (style)
            {
                case ButtonStyle.Gold: return new ButtonColors(Palette.Hex(0xFFD23F), Palette.Hex(0xD4A514), Palette.Hex(0x2A2350));
                case ButtonStyle.Play: return new ButtonColors(Palette.Hex(0xF0641E), Palette.Hex(0xB8460F), white);
                case ButtonStyle.Success: return new ButtonColors(Palette.Hex(0x12A67A), Palette.Hex(0x0B7555), white);
                case ButtonStyle.Info: return new ButtonColors(Palette.Hex(0x1C9AD6), Palette.Hex(0x13709E), white);
                case ButtonStyle.Danger: return new ButtonColors(Palette.Hex(0xE5484D), Palette.Hex(0xB02D32), white);
                case ButtonStyle.Pvp: return new ButtonColors(Palette.Hex(0xE83E8C), Palette.Hex(0xB02467), white);
                case ButtonStyle.Training: return new ButtonColors(Palette.Hex(0x8E7A5B), Palette.Hex(0x65563F), white);
                case ButtonStyle.Locked: return new ButtonColors(Palette.Hex(0xA9A4C9), Palette.Hex(0x7D78A3), white);
                default: return new ButtonColors(Palette.Hex(0x6D4AFF), Palette.Hex(0x4A2BD1), white);
            }
        }
    }
}
