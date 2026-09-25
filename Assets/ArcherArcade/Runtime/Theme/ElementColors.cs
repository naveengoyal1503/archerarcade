using ArcherArcade.Logic;
using UnityEngine;

namespace ArcherArcade.Theme
{
    /// <summary>Element main + glow colors (DESIGN_TOKENS §3). Always shown together with the element's icon.</summary>
    public static class ElementColors
    {
        public static Color Main(Element e)
        {
            switch (e)
            {
                case Element.Fire: return Palette.Hex(0xFF8A3D);
                case Element.Electric: return Palette.Hex(0xFFD23F);
                case Element.Bomb: return Palette.Hex(0xFF5A6E);
                case Element.Ice: return Palette.Hex(0x7FD6F7);
                case Element.Poison: return Palette.Hex(0x9BE15D);
                default: return Palette.Hex(0xFFFFFF);
            }
        }

        public static Color Glow(Element e)
        {
            switch (e)
            {
                case Element.Fire: return Palette.Hex(0xFFD2A6);
                case Element.Electric: return Palette.Hex(0xFFF3C4);
                case Element.Bomb: return Palette.Hex(0xFFB800);
                case Element.Ice: return Palette.Hex(0xE6F6FF);
                case Element.Poison: return Palette.Hex(0xC6FF4A);
                default: return Palette.Hex(0xFFFFFF);
            }
        }
    }
}
