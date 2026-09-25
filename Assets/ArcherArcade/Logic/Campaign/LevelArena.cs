using System.Collections.Generic;

namespace ArcherArcade.Logic.Campaign
{
    /// <summary>
    /// Builder for a level's geometry: floating islands, props, spawn points. Units are metres; the player stands
    /// at x = 0 facing right, the islands' tops are at y = 0 unless raised.
    /// </summary>
    public sealed class LevelArena
    {
        public readonly ArenaLayout Layout = new ArenaLayout();
        public readonly List<PropSpec> Props = new List<PropSpec>();
        public readonly OpponentSpec[] Opponents;
        public Vec2 PlayerFeet = new Vec2(0.0, 0.0);
        public int PlayerStandOnProp = -1;
        double _maxX;

        public LevelArena(OpponentSpec[] opponents)
        {
            Opponents = opponents ?? new OpponentSpec[0];
            Island(0.0, 6.0);
        }

        /// <summary>A floating island (ground box 2 m deep) centred at x with its top at <paramref name="top"/>.</summary>
        public void Island(double centerX, double width, double top = 0.0)
        {
            Layout.Grounds.Add(Shape.BoxFromTop(centerX, top, width, 2.0));
            Extend(centerX + width * 0.5);
        }

        /// <summary>Places opponent <paramref name="index"/> standing at x on top of height <paramref name="groundY"/>.</summary>
        public void Opponent(int index, double x, double groundY = 0.0)
        {
            Opponents[index].Feet = new Vec2(x, groundY);
            Extend(x);
        }

        public int Add(PropSpec p)
        {
            Props.Add(p);
            return Props.Count - 1;
        }

        /// <summary>Solid wall standing on <paramref name="bottom"/>.</summary>
        public int Wall(double x, double bottom, double height, double halfWidth = 0.25)
        {
            return Add(PropSpec.Of(PropKind.Wall, Shape.Box(new Vec2(x, bottom + height * 0.5), new Vec2(halfWidth, height * 0.5))));
        }

        /// <summary>Crate tower; returns the index of its bottom crate.</summary>
        public int Tower(int towerId, double x, double groundY, int crates, int tntIndex = -1, double crateSize = 0.9)
        {
            return TowerBuilder.Add(Props, towerId, x, groundY, crates, crateSize, tntIndex);
        }

        public int Crate(double x, double groundY, double size = 0.9)
        {
            return Add(PropSpec.Of(PropKind.Crate, Shape.Box(new Vec2(x, groundY + size * 0.5), new Vec2(size * 0.5, size * 0.5))));
        }

        public int Barrel(double x, double groundY)
        {
            return Add(PropSpec.Of(PropKind.ExplosiveBarrel, Shape.Circle(new Vec2(x, groundY + 0.45), 0.45)));
        }

        public int Target(double x, double y, double radius = 0.45)
        {
            Extend(x);
            return Add(PropSpec.Of(PropKind.Target, Shape.Circle(new Vec2(x, y), radius)));
        }

        /// <summary>Target hanging <paramref name="length"/> m below a pivot, swinging ±amplitude.</summary>
        public int SwingTarget(double pivotX, double pivotY, double length, double amplitudeDeg, double period, double phaseDeg, double radius = 0.45)
        {
            Extend(pivotX);
            return Add(new PropSpec
            {
                Kind = PropKind.Target,
                Shape = Shape.Circle(new Vec2(pivotX, pivotY - length), radius),
                Motion = PropMotion.Swing(new Vec2(pivotX, pivotY), amplitudeDeg, period, phaseDeg)
            });
        }

        public int MovingTarget(double x, double y, Vec2 offsetA, Vec2 offsetB, double period, double radius = 0.45)
        {
            Extend(x);
            return Add(new PropSpec
            {
                Kind = PropKind.Target,
                Shape = Shape.Circle(new Vec2(x, y), radius),
                Motion = PropMotion.PingPong(offsetA, offsetB, period)
            });
        }

        /// <summary>Flat bounce pad or pad wall (box).</summary>
        public int Pad(double centerX, double centerY, double halfWidth, double halfHeight)
        {
            return Add(PropSpec.Of(PropKind.BouncePad, Shape.Box(new Vec2(centerX, centerY), new Vec2(halfWidth, halfHeight))));
        }

        /// <summary>Friendly dummy standing on <paramref name="groundY"/>; returns its index (the apple is added separately).</summary>
        public int Dummy(double x, double groundY)
        {
            return Add(PropSpec.Of(PropKind.Dummy, Shape.Box(new Vec2(x, groundY + 0.8), new Vec2(0.25, 0.8))));
        }

        /// <summary>Apple balanced on top of a dummy standing on <paramref name="groundY"/>.</summary>
        public int Apple(double x, double groundY)
        {
            return Add(PropSpec.Of(PropKind.Apple, Shape.Circle(new Vec2(x, groundY + 1.6 + 0.16), 0.16)));
        }

        public int Rope(double x, double bottom, double top)
        {
            return Add(PropSpec.Of(PropKind.Rope, Shape.Box(new Vec2(x, (bottom + top) * 0.5), new Vec2(0.06, (top - bottom) * 0.5))));
        }

        /// <summary>Platform that steps between two offsets once per turn over <paramref name="cycle"/> turns.</summary>
        public int Platform(double centerX, double topY, double halfWidth, Vec2 offsetA, Vec2 offsetB, int cycle)
        {
            Extend(centerX + halfWidth);
            return Add(new PropSpec
            {
                Kind = PropKind.Platform,
                Shape = Shape.Box(new Vec2(centerX, topY - 0.2), new Vec2(halfWidth, 0.2)),
                TurnOffsetA = offsetA,
                TurnOffsetB = offsetB,
                TurnCycle = cycle
            });
        }

        void Extend(double x)
        {
            if (x > _maxX) _maxX = x;
            Layout.MinX = -30.0;
            Layout.MaxX = _maxX + 30.0;
        }
    }
}
