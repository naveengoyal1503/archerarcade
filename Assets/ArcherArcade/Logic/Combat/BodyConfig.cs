namespace ArcherArcade.Logic
{
    /// <summary>
    /// Hit-zone shapes relative to the archer's feet (x = 0 at the feet, y up), for the default standing pose.
    /// Per-archer poses (big-head Giant Brute etc.) use their own instance.
    /// </summary>
    public sealed class BodyConfig
    {
        public Vec2 HeadCenter = new Vec2(0.0, 1.62);
        public double HeadRadius = 0.30;

        public Vec2 BodyBottom = new Vec2(0.0, 0.88);
        public Vec2 BodyTop = new Vec2(0.0, 1.25);
        public double BodyRadius = 0.30;

        public Vec2 LegsBottom = new Vec2(0.0, 0.12);
        public Vec2 LegsTop = new Vec2(0.0, 0.70);
        public double LegsRadius = 0.20;

        /// <summary>Zone shape in world space for an archer standing at <paramref name="feet"/>.</summary>
        public Shape ZoneShape(HitZone zone, Vec2 feet)
        {
            switch (zone)
            {
                case HitZone.Head: return Shape.Circle(feet + HeadCenter, HeadRadius);
                case HitZone.Legs: return Shape.Capsule(feet + LegsBottom, feet + LegsTop, LegsRadius);
                default: return Shape.Capsule(feet + BodyBottom, feet + BodyTop, BodyRadius);
            }
        }

        public Vec2 ZoneCenter(HitZone zone, Vec2 feet) => ZoneShape(zone, feet).Center;
    }
}
