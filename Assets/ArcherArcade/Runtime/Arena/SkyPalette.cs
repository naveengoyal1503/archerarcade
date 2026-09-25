using ArcherArcade.Logic.Campaign;
using UnityEngine;

namespace ArcherArcade.Arena
{
    /// <summary>
    /// World 1 lighting per time of day (sunny forest → dusk → night, LEVELS.md) and theme. Sky colours for day and
    /// the dark theme come from the prototype's match canvas (#8FD3FF → #E6F6FF, #1E1A45 → #4A3478).
    /// </summary>
    public readonly struct SkyPalette
    {
        public readonly Color Top, Bottom, World, Far, Chars, Sun;
        public readonly bool Moon, Stars, Fireflies;

        SkyPalette(Color top, Color bottom, Color world, Color far, Color chars, Color sun, bool moon, bool stars, bool fireflies)
        {
            Top = top;
            Bottom = bottom;
            World = world;
            Far = far;
            Chars = chars;
            Sun = sun;
            Moon = moon;
            Stars = stars;
            Fireflies = fireflies;
        }

        static Color H(uint rgb) => Theme.Palette.Hex(rgb);

        public static SkyPalette For(TimeOfDay time, bool darkTheme)
        {
            if (darkTheme && time == TimeOfDay.Day) time = TimeOfDay.Dusk;
            switch (time)
            {
                case TimeOfDay.Dusk:
                    return darkTheme
                        ? new SkyPalette(H(0x1E1A45), H(0x4A3478), new Color(0.78f, 0.76f, 0.95f), new Color(0.45f, 0.42f, 0.72f), new Color(0.9f, 0.9f, 1f), H(0xFFF3C4), true, true, true)
                        : new SkyPalette(H(0x6D5BD0), H(0xFFB38A), new Color(1f, 0.88f, 0.8f), new Color(0.86f, 0.62f, 0.72f), new Color(1f, 0.93f, 0.88f), H(0xFFB86B), false, false, true);
                case TimeOfDay.Night:
                    return new SkyPalette(H(0x151238), H(0x3A2766), new Color(0.62f, 0.64f, 0.88f), new Color(0.3f, 0.3f, 0.55f), new Color(0.84f, 0.84f, 1f), H(0xFFF3C4), true, true, true);
                default:
                    return new SkyPalette(H(0x8FD3FF), H(0xE6F6FF), Color.white, new Color(0.82f, 0.93f, 1f), Color.white, H(0xFFE27A), false, false, false);
            }
        }
    }
}
