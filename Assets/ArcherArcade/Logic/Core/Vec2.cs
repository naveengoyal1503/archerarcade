namespace ArcherArcade.Logic
{
    /// <summary>2D vector in world units (1 unit ≈ 1 m, y up). Double precision for deterministic simulation.</summary>
    [System.Serializable]
    public struct Vec2
    {
        public double X;
        public double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static readonly Vec2 Zero = new Vec2(0.0, 0.0);

        public double LengthSq => X * X + Y * Y;
        public double Length => DetMath.Sqrt(LengthSq);

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator *(double s, Vec2 a) => new Vec2(a.X * s, a.Y * s);

        public static double Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
        public static double DistanceSq(Vec2 a, Vec2 b) => (a - b).LengthSq;
        public static double Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public static Vec2 Lerp(Vec2 a, Vec2 b, double t) => new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        /// <summary>Rotates counter-clockwise by the given angle (deterministic trig).</summary>
        public Vec2 Rotated(double degrees)
        {
            double c = DetMath.CosDeg(degrees);
            double s = DetMath.SinDeg(degrees);
            return new Vec2(X * c - Y * s, X * s + Y * c);
        }

        public override string ToString() => "(" + X.ToString("0.###") + ", " + Y.ToString("0.###") + ")";
    }
}
