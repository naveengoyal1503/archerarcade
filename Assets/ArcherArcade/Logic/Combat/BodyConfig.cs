namespace ArcherArcade.Logic
{
    /// <summary>
    /// Hit-zone shapes relative to the archer's feet (x = 0 at the feet, y up), for the default standing pose at
    /// scale 1. Big archers (the boss) scale every zone around the feet. The weak spot (boss chest knot) sits on
    /// the chest surface facing the opponent.
    /// </summary>
    [System.Serializable]
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

        /// <summary>Weak spot centre (x along facing) and radius; it pokes out of the chest so arrows reach it first.</summary>
        public Vec2 WeakSpotCenter = new Vec2(0.26, 1.06);
        public double WeakSpotRadius = 0.16;

        /// <summary>Zone shape in world space for an archer standing at <paramref name="feet"/>.</summary>
        public Shape ZoneShape(HitZone zone, Vec2 feet, double scale = 1.0, int facing = 1)
        {
            switch (zone)
            {
                case HitZone.Head: return Shape.Circle(feet + HeadCenter * scale, HeadRadius * scale);
                case HitZone.Legs: return Shape.Capsule(feet + LegsBottom * scale, feet + LegsTop * scale, LegsRadius * scale);
                case HitZone.WeakSpot:
                    return Shape.Circle(feet + new Vec2(WeakSpotCenter.X * facing, WeakSpotCenter.Y) * scale, WeakSpotRadius * scale);
                default: return Shape.Capsule(feet + BodyBottom * scale, feet + BodyTop * scale, BodyRadius * scale);
            }
        }

        public Vec2 ZoneCenter(HitZone zone, Vec2 feet, double scale = 1.0, int facing = 1) => ZoneShape(zone, feet, scale, facing).Center;

        /// <summary>Zone shape for a fighter (uses its archer's scale and facing).</summary>
        public Shape ZoneShape(HitZone zone, Fighter f) => ZoneShape(zone, f.Feet, f.Def.BodyScale, f.Facing);

        public Vec2 ZoneCenter(HitZone zone, Fighter f) => ZoneShape(zone, f).Center;
    }
}
