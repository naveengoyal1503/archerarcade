namespace ArcherArcade.Logic
{
    /// <summary>
    /// damage = tip damage × (archer base / reference) × (1 + 4 % per level above 1) × zone × element bonus,
    /// rounded half away from zero; any hit with a positive multiplier does at least 1.
    /// </summary>
    public static class Damage
    {
        public static int Compute(double tipDamage, ArcherDef archer, int level, double zoneMultiplier,
            Element tipElement, DamageConfig cfg)
        {
            if (tipDamage <= 0.0 || zoneMultiplier <= 0.0) return 0;
            double archerScale = archer.BaseDamage / cfg.ReferenceDamage;
            double levelScale = 1.0 + cfg.DamagePerLevel * (level < 1 ? 0 : level - 1);
            double element = tipElement != Element.None && tipElement == archer.Element ? 1.0 + cfg.ElementBonus : 1.0;
            int value = DetMath.RoundToInt(tipDamage * archerScale * levelScale * zoneMultiplier * element);
            return value < 1 ? 1 : value;
        }

        /// <summary>Ability arrows: the damage is already the archer's own; only the upgrade level and zone scale it.</summary>
        public static int ComputeFixed(double damage, int level, double zoneMultiplier, DamageConfig cfg)
        {
            if (damage <= 0.0 || zoneMultiplier <= 0.0) return 0;
            double levelScale = 1.0 + cfg.DamagePerLevel * (level < 1 ? 0 : level - 1);
            int value = DetMath.RoundToInt(damage * levelScale * zoneMultiplier);
            return value < 1 ? 1 : value;
        }

        public static int MaxHp(ArcherDef archer, int level, DamageConfig cfg)
        {
            return archer.BaseHp + cfg.HpPerLevel * (level < 1 ? 0 : level - 1);
        }
    }
}
