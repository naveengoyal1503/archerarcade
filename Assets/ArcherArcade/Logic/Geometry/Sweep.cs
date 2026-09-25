namespace ArcherArcade.Logic
{
    /// <summary>
    /// Earliest contact of a moving point (segment P0→P1, one simulation step) with a shape. Returns the fraction
    /// t in [0, 1] along the segment. A start point already inside a shape reports t = 0.
    /// Uses only +,-,*,/ and sqrt, so results are bit-identical on every device.
    /// </summary>
    public static class Sweep
    {
        public static bool Segment(Vec2 p0, Vec2 p1, in Shape shape, out double t)
        {
            switch (shape.Kind)
            {
                case ShapeKind.Circle: return SegmentCircle(p0, p1, shape.A, shape.Radius, out t);
                case ShapeKind.Capsule: return SegmentCapsule(p0, p1, shape.A, shape.B, shape.Radius, out t);
                default: return SegmentBox(p0, p1, shape.A, shape.HalfSize, out t);
            }
        }

        public static bool SegmentCircle(Vec2 p0, Vec2 p1, Vec2 center, double radius, out double t)
        {
            t = 0.0;
            Vec2 d = p1 - p0;
            Vec2 f = p0 - center;
            double c = f.LengthSq - radius * radius;
            if (c <= 0.0) return true;
            double a = d.LengthSq;
            if (a <= 0.0) return false;
            double b = 2.0 * Vec2.Dot(f, d);
            if (b >= 0.0) return false; // moving away
            double disc = b * b - 4.0 * a * c;
            if (disc < 0.0) return false;
            double hit = (-b - DetMath.Sqrt(disc)) / (2.0 * a);
            if (hit < 0.0 || hit > 1.0) return false;
            t = hit;
            return true;
        }

        public static bool SegmentBox(Vec2 p0, Vec2 p1, Vec2 center, Vec2 half, out double t)
        {
            t = 0.0;
            double tMin = 0.0;
            double tMax = 1.0;
            if (!Slab(p0.X - center.X, p1.X - p0.X, half.X, ref tMin, ref tMax)) return false;
            if (!Slab(p0.Y - center.Y, p1.Y - p0.Y, half.Y, ref tMin, ref tMax)) return false;
            t = tMin;
            return true;
        }

        public static bool SegmentCapsule(Vec2 p0, Vec2 p1, Vec2 a, Vec2 b, double radius, out double t)
        {
            Vec2 axis = b - a;
            double len = axis.Length;
            if (len <= 1e-12) return SegmentCircle(p0, p1, a, radius, out t);

            bool hit = false;
            t = double.MaxValue;
            double tc;
            if (SegmentCircle(p0, p1, a, radius, out tc)) { hit = true; t = tc; }
            if (SegmentCircle(p0, p1, b, radius, out tc) && tc < t) { hit = true; t = tc; }

            // Middle rectangle in the capsule's local frame: x along the axis [0, len], y across [-r, r].
            Vec2 u = axis * (1.0 / len);
            Vec2 n = new Vec2(-u.Y, u.X);
            Vec2 r0 = p0 - a;
            Vec2 r1 = p1 - a;
            Vec2 l0 = new Vec2(Vec2.Dot(r0, u), Vec2.Dot(r0, n));
            Vec2 l1 = new Vec2(Vec2.Dot(r1, u), Vec2.Dot(r1, n));
            if (SegmentBox(l0, l1, new Vec2(len * 0.5, 0.0), new Vec2(len * 0.5, radius), out tc) && tc < t)
            {
                hit = true;
                t = tc;
            }
            if (!hit) t = 0.0;
            return hit;
        }

        public static double DistancePointSegment(Vec2 p, Vec2 a, Vec2 b)
        {
            Vec2 ab = b - a;
            double lenSq = ab.LengthSq;
            double k = lenSq <= 0.0 ? 0.0 : DetMath.Clamp01(Vec2.Dot(p - a, ab) / lenSq);
            return Vec2.Distance(p, a + ab * k);
        }

        static bool Slab(double start, double delta, double half, ref double tMin, ref double tMax)
        {
            if (delta == 0.0) return start >= -half && start <= half;
            double inv = 1.0 / delta;
            double t1 = (-half - start) * inv;
            double t2 = (half - start) * inv;
            if (t1 > t2)
            {
                double tmp = t1;
                t1 = t2;
                t2 = tmp;
            }
            if (t1 > tMin) tMin = t1;
            if (t2 < tMax) tMax = t2;
            return tMin <= tMax;
        }
    }
}
