namespace ArcherArcade.Logic
{
    /// <summary>
    /// Continuous motion of a prop as a pure function of the match clock (seconds), so a moving target is in the
    /// same place for the same clock on every device.
    /// </summary>
    public struct PropMotion
    {
        public MotionKind Kind;

        /// <summary>Swing: pivot point, max angle, and phase (degrees). The rest shape hangs straight down.</summary>
        public Vec2 Pivot;
        public double AmplitudeDeg;
        public double PhaseDeg;

        /// <summary>PingPong: offsets from the rest shape at both ends.</summary>
        public Vec2 OffsetA;
        public Vec2 OffsetB;

        /// <summary>Seconds for one full cycle (there and back).</summary>
        public double PeriodSeconds;

        public static PropMotion Swing(Vec2 pivot, double amplitudeDeg, double periodSeconds, double phaseDeg = 0.0)
        {
            return new PropMotion
            {
                Kind = MotionKind.Swing, Pivot = pivot, AmplitudeDeg = amplitudeDeg, PeriodSeconds = periodSeconds,
                PhaseDeg = phaseDeg
            };
        }

        public static PropMotion PingPong(Vec2 offsetA, Vec2 offsetB, double periodSeconds)
        {
            return new PropMotion { Kind = MotionKind.PingPong, OffsetA = offsetA, OffsetB = offsetB, PeriodSeconds = periodSeconds };
        }

        /// <summary>The rest shape moved to where it is at <paramref name="clock"/>.</summary>
        public Shape Apply(Shape rest, double clock)
        {
            if (Kind == MotionKind.None || PeriodSeconds <= 0.0) return rest;
            double cycles = clock / PeriodSeconds;
            if (Kind == MotionKind.Swing)
            {
                double angle = AmplitudeDeg * DetMath.Sin(DetMath.TwoPi * cycles + PhaseDeg * DetMath.Deg2Rad);
                Shape s = rest;
                s.A = Pivot + (rest.A - Pivot).Rotated(angle);
                s.B = Pivot + (rest.B - Pivot).Rotated(angle);
                return s;
            }
            return rest.Translated(Vec2.Lerp(OffsetA, OffsetB, Triangle(cycles)));
        }

        /// <summary>0 → 1 → 0 over one cycle.</summary>
        public static double Triangle(double cycles)
        {
            double f = cycles - System.Math.Floor(cycles);
            return f < 0.5 ? f * 2.0 : 2.0 - f * 2.0;
        }
    }
}
