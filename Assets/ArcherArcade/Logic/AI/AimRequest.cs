namespace ArcherArcade.Logic
{
    /// <summary>What the solver should hit, from where, in which conditions.</summary>
    public struct AimRequest
    {
        public Vec2 Origin;
        public int Facing;
        public Vec2 Target;
        public double Wind;
        public double GravityScale;

        /// <summary>Angle search step in degrees (0.5 by default).</summary>
        public double AngleStepDeg;

        /// <summary>Search from the steepest angle down (lob) instead of flattest first.</summary>
        public bool PreferHighArc;

        /// <summary>Match clock at release (places moving props during validation).</summary>
        public double Clock;

        /// <summary>Launch speed and wind multipliers of the arrow (see TipDef / ArcherDef).</summary>
        public double SpeedScale;
        public double WindScale;

        public static AimRequest Create(Vec2 origin, int facing, Vec2 target, double wind, double gravityScale = 1.0)
        {
            return new AimRequest
            {
                Origin = origin,
                Facing = facing,
                Target = target,
                Wind = wind,
                GravityScale = gravityScale,
                AngleStepDeg = 0.5,
                PreferHighArc = false,
                SpeedScale = 1.0,
                WindScale = 1.0
            };
        }
    }
}
