namespace ArcherArcade.Logic
{
    /// <summary>
    /// Collision shape in world units. Circle: center A, Radius. Capsule: segment A–B, Radius.
    /// Box: axis-aligned, center A, half size HalfSize.
    /// </summary>
    public struct Shape
    {
        public ShapeKind Kind;
        public Vec2 A;
        public Vec2 B;
        public double Radius;
        public Vec2 HalfSize;

        public static Shape Circle(Vec2 center, double radius)
        {
            return new Shape { Kind = ShapeKind.Circle, A = center, B = center, Radius = radius };
        }

        public static Shape Capsule(Vec2 a, Vec2 b, double radius)
        {
            return new Shape { Kind = ShapeKind.Capsule, A = a, B = b, Radius = radius };
        }

        public static Shape Box(Vec2 center, Vec2 halfSize)
        {
            return new Shape { Kind = ShapeKind.Box, A = center, HalfSize = halfSize };
        }

        /// <summary>Box from its top edge: handy for islands and walls standing on the ground.</summary>
        public static Shape BoxFromTop(double centerX, double topY, double width, double height)
        {
            return Box(new Vec2(centerX, topY - height * 0.5), new Vec2(width * 0.5, height * 0.5));
        }

        public Shape Translated(Vec2 offset)
        {
            Shape s = this;
            s.A = A + offset;
            s.B = B + offset;
            return s;
        }

        /// <summary>Center point (for aiming and effect placement).</summary>
        public Vec2 Center => Kind == ShapeKind.Capsule ? Vec2.Lerp(A, B, 0.5) : A;

        /// <summary>
        /// Outward surface normal at a contact point on (or near) the shape. Boxes use the face the point is
        /// closest to (relative to the half size); round shapes use the direction from the nearest axis point.
        /// </summary>
        public Vec2 NormalAt(Vec2 p)
        {
            if (Kind == ShapeKind.Box)
            {
                double dx = p.X - A.X;
                double dy = p.Y - A.Y;
                double nx = HalfSize.X > 0.0 ? System.Math.Abs(dx) / HalfSize.X : 0.0;
                double ny = HalfSize.Y > 0.0 ? System.Math.Abs(dy) / HalfSize.Y : 0.0;
                if (nx > ny) return new Vec2(dx >= 0.0 ? 1.0 : -1.0, 0.0);
                return new Vec2(0.0, dy >= 0.0 ? 1.0 : -1.0);
            }
            Vec2 ab = B - A;
            double lenSq = ab.LengthSq;
            double k = lenSq <= 0.0 ? 0.0 : DetMath.Clamp01(Vec2.Dot(p - A, ab) / lenSq);
            Vec2 d = p - (A + ab * k);
            double len = d.Length;
            return len <= 1e-12 ? new Vec2(0.0, 1.0) : d * (1.0 / len);
        }

        /// <summary>Distance from a point to the shape's surface (0 inside).</summary>
        public double DistanceTo(Vec2 p)
        {
            switch (Kind)
            {
                case ShapeKind.Box:
                {
                    double dx = System.Math.Abs(p.X - A.X) - HalfSize.X;
                    double dy = System.Math.Abs(p.Y - A.Y) - HalfSize.Y;
                    if (dx <= 0.0 && dy <= 0.0) return 0.0;
                    double ox = dx > 0.0 ? dx : 0.0;
                    double oy = dy > 0.0 ? dy : 0.0;
                    return DetMath.Sqrt(ox * ox + oy * oy);
                }
                default:
                {
                    double d = Sweep.DistancePointSegment(p, A, B) - Radius;
                    return d > 0.0 ? d : 0.0;
                }
            }
        }
    }
}
