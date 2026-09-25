using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>
    /// Static arena geometry: floating islands (ground) and walls, plus the void and side limits. Props with
    /// behaviour (crates, towers, pads…) arrive in Phase 4.
    /// </summary>
    public sealed class ArenaLayout
    {
        public readonly List<Shape> Grounds = new List<Shape>();
        public readonly List<Shape> Walls = new List<Shape>();

        /// <summary>An arrow below this height has fallen into the void.</summary>
        public double KillY = -12.0;

        public double MinX = -30.0;
        public double MaxX = 70.0;

        /// <summary>Two islands (top at y = 0) centred on the archers, each <paramref name="islandWidth"/> wide.</summary>
        public static ArenaLayout TwoIslands(double leftX, double rightX, double islandWidth = 6.0)
        {
            var a = new ArenaLayout();
            a.Grounds.Add(Shape.BoxFromTop(leftX, 0.0, islandWidth, 2.0));
            a.Grounds.Add(Shape.BoxFromTop(rightX, 0.0, islandWidth, 2.0));
            a.MinX = leftX - 30.0;
            a.MaxX = rightX + 30.0;
            return a;
        }
    }
}
