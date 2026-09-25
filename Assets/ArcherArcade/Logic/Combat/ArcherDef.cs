namespace ArcherArcade.Logic
{
    /// <summary>
    /// One archer or enemy type: level-1 stats, element, passive, ability and behaviour (GAME_DESIGN §4, §4.1).
    /// Filled by the Runtime archer config; defaults in <see cref="ArcherTable"/> and <see cref="EnemyTable"/>.
    /// </summary>
    public sealed class ArcherDef
    {
        public string Id = "ranger";

        /// <summary>Display name (proper noun, same in every language), e.g. "Hunter Moss".</summary>
        public string Name = "Ranger";
        public Element Element = Element.None;
        public int BaseHp = 100;
        public double BaseDamage = 25.0;

        public AbilityKind Ability = AbilityKind.None;
        public ArcherUnlock Unlock = ArcherUnlock.Owned;
        public int MaxLevel = 10;

        /// <summary>Ranger: trajectory preview longer by this share (0.10 = +10 %).</summary>
        public double PreviewBonus;

        /// <summary>Fire Archer: every hit adds burn.</summary>
        public int PassiveBurnPerTurn;
        public int PassiveBurnTurns;

        /// <summary>Electric Archer: hits chain to one other target within the radius.</summary>
        public double PassiveChainFraction;
        public double PassiveChainRadius;

        /// <summary>Bomb Archer: arrows explode on impact.</summary>
        public double PassiveSplashDamage;
        public double PassiveSplashRadius;

        // Enemy behaviour (GAME_DESIGN §4.1).

        /// <summary>Crossbow Scout: flatter, faster shots.</summary>
        public double ShotSpeedScale = 1.0;
        public double ShotGravityScale = 1.0;

        /// <summary>Twin Shooter: aimed shots per turn.</summary>
        public int ShotsPerTurn = 1;

        /// <summary>Healer Druid: heals this much at the start of every Nth own turn.</summary>
        public int HealAmount;
        public int HealEveryTurns;

        /// <summary>Bubble caster: casts a shield bubble at the start of every Nth own turn.</summary>
        public int BubbleEveryTurns;

        /// <summary>Shield Bearer: carries a wooden shield in front (added as a prop).</summary>
        public bool CarriesShield;

        // Bosses (GAME_DESIGN §6.4).

        /// <summary>Size of the archer's hit zones (the boss is bigger).</summary>
        public double BodyScale = 1.0;

        /// <summary>Has a weak spot on the chest (×2.5 when not covered).</summary>
        public bool HasWeakSpot;

        /// <summary>Two wooden shields that rotate every turn, leaving a different zone open each time.</summary>
        public bool RotatingShields;

        /// <summary>Grows a vine wall in front of the opponent at the start of every Nth own turn.</summary>
        public int VineWallEveryTurns;

        /// <summary>Shoots twice per turn once HP is at or below this share of max HP (0 = never).</summary>
        public double EnrageBelow;
    }
}
