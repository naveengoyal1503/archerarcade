namespace ArcherArcade.Logic
{
    /// <summary>Damage tuning (GAME_DESIGN §3.3, §4, §5).</summary>
    public sealed class DamageConfig
    {
        public double HeadMultiplier = 2.0;
        public double BodyMultiplier = 1.0;
        public double LegsMultiplier = 0.6;
        public double WeakSpotMultiplier = 2.5;

        /// <summary>Tip damage values assume an archer with this base damage (Ranger, 25).</summary>
        public double ReferenceDamage = 25.0;

        /// <summary>Per upgrade level above 1: +4 % damage, +4 HP.</summary>
        public double DamagePerLevel = 0.04;
        public int HpPerLevel = 4;

        /// <summary>Archer using a tip of its own element: +20 %.</summary>
        public double ElementBonus = 0.20;

        public double ZoneMultiplier(HitZone zone)
        {
            switch (zone)
            {
                case HitZone.Head: return HeadMultiplier;
                case HitZone.Body: return BodyMultiplier;
                case HitZone.Legs: return LegsMultiplier;
                case HitZone.WeakSpot: return WeakSpotMultiplier;
                default: return 0.0;
            }
        }
    }
}
