namespace ArcherArcade.Logic
{
    /// <summary>How one prop is placed in a level. Shapes are in world units at rest.</summary>
    public sealed class PropSpec
    {
        public PropKind Kind;

        /// <summary>
        /// Rest shape in world space. For a Shield it is relative to its owner's feet (the shield follows the
        /// owner when knocked back).
        /// </summary>
        public Shape Shape;

        /// <summary>Hits needed to break a standalone crate; 0 = PropConfig default.</summary>
        public int Hits;

        /// <summary>Crate tower this prop belongs to (−1 = none) and its position in the stack (0 = bottom).</summary>
        public int Tower = -1;
        public int StackIndex;

        /// <summary>Continuous motion (swinging / moving targets).</summary>
        public PropMotion Motion;

        /// <summary>Moving platform: offsets at both ends and turns for a full there-and-back cycle.</summary>
        public Vec2 TurnOffsetA;
        public Vec2 TurnOffsetB;
        public int TurnCycle;

        /// <summary>Fighter index carrying this shield (Shield only).</summary>
        public int ShieldOwner = -1;

        /// <summary>Boss rotating shield: which of the two shields this is (0 or 1), −1 for a normal shield.</summary>
        public int RotatingSlot = -1;

        /// <summary>Fighter who grows this vine wall (VineWall only); it starts hidden.</summary>
        public int VineOwner = -1;

        /// <summary>Target that stays up after a hit (Training Range boards).</summary>
        public bool Durable;

        public static PropSpec Of(PropKind kind, Shape shape) => new PropSpec { Kind = kind, Shape = shape };
    }
}
