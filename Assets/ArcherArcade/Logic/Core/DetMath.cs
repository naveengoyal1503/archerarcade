using System;

namespace ArcherArcade.Logic
{
    /// <summary>
    /// Deterministic math helpers. System.Math.Sin/Cos are not guaranteed bit-identical across CPUs and runtimes,
    /// so trig here is a fixed polynomial built only from +,-,*,/ (IEEE-exact) and Math.Floor/Sqrt (correctly
    /// rounded). Everything that decides a shot's result goes through this class.
    /// </summary>
    public static class DetMath
    {
        public const double Pi = 3.141592653589793;
        public const double TwoPi = 6.283185307179586;
        public const double HalfPi = 1.5707963267948966;
        public const double Deg2Rad = Pi / 180.0;
        public const double Rad2Deg = 180.0 / Pi;

        public static double Sin(double x)
        {
            // Reduce to [-pi, pi].
            x -= TwoPi * Math.Floor((x + Pi) / TwoPi);
            // Reduce to [-pi/2, pi/2] using sin(pi - x) = sin(x).
            if (x > HalfPi) x = Pi - x;
            else if (x < -HalfPi) x = -Pi - x;
            double x2 = x * x;
            // Taylor series to x^19: error < 1e-15 on [-pi/2, pi/2].
            double t = x2 / 342.0;          // 18*19
            t = x2 / 272.0 * (1.0 - t);     // 16*17
            t = x2 / 210.0 * (1.0 - t);     // 14*15
            t = x2 / 156.0 * (1.0 - t);     // 12*13
            t = x2 / 110.0 * (1.0 - t);     // 10*11
            t = x2 / 72.0 * (1.0 - t);      // 8*9
            t = x2 / 42.0 * (1.0 - t);      // 6*7
            t = x2 / 20.0 * (1.0 - t);      // 4*5
            t = x2 / 6.0 * (1.0 - t);       // 2*3
            return x * (1.0 - t);
        }

        public static double Cos(double x) => Sin(x + HalfPi);

        public static double SinDeg(double degrees) => Sin(degrees * Deg2Rad);
        public static double CosDeg(double degrees) => Cos(degrees * Deg2Rad);

        public static double Sqrt(double x) => x <= 0.0 ? 0.0 : Math.Sqrt(x);

        public static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
        public static double Clamp01(double v) => Clamp(v, 0.0, 1.0);
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        public static double EaseOutQuad(double t) => t * (2.0 - t);

        /// <summary>Rounds half away from zero (never banker's rounding) so damage numbers are predictable.</summary>
        public static int RoundToInt(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);
    }
}
