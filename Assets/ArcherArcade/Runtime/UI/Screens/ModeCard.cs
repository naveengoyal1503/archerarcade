using System;

namespace ArcherArcade.UI
{
    /// <summary>One game mode tile (Home cards, Modes grid): name, sub-line, icon, colours and where it goes.</summary>
    public sealed class ModeCard
    {
        public string Name;
        public string Sub;
        public string Icon;
        public uint Color;
        public uint Shade;
        public Action Open;
    }
}
