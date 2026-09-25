namespace ArcherArcade.Logic
{
    /// <summary>Numbers for one arrow tip (GAME_DESIGN §5). Filled by the Runtime config; defaults in TipTable.</summary>
    public sealed class TipDef
    {
        public ArrowTip Tip;
        public Element Element;

        /// <summary>Ammo per match; −1 = unlimited.</summary>
        public int Ammo = -1;

        public double Damage = 25.0;
        public double GravityScale = 1.0;

        public int BurnPerTurn;
        public int BurnTurns;
        public int PoisonPerTurn;
        public int PoisonTurns;

        public double ChainFraction;
        public double ChainRadius;

        public double SplashDamage;
        public double SplashRadius;

        public int SplitCount;
        public double SplitSpreadDeg;

        /// <summary>Seconds removed from the target's next turn timer.</summary>
        public double StunSeconds;

        /// <summary>Target's next draw is this much slower (0.3 = 30 %).</summary>
        public double FreezeDrawSlow;

        /// <summary>Pops shield bubbles instantly (Electric).</summary>
        public bool PopsBubbles;

        /// <summary>Knocks wooden shields down (Heavy, Bomb).</summary>
        public bool KnocksShields;

        /// <summary>Pushes the archer it hits back (Bomb).</summary>
        public bool Knockback;

        /// <summary>Arrows fired at once in a fan (Triple Shot, Multi Arrow booster), spread each side in degrees.</summary>
        public int LaunchCount = 1;
        public double LaunchSpreadDeg;

        /// <summary>Launch speed and wind effect multipliers (Storm Bolt: faster, ignores wind).</summary>
        public double SpeedScale = 1.0;
        public double WindScale = 1.0;

        /// <summary>How many times a chain jumps on (Storm Bolt: 2).</summary>
        public int ChainCount = 1;

        /// <summary>
        /// Ability arrow: its damage numbers are already the archer's own (no archer-base scaling, no element
        /// bonus); only the upgrade level scales it.
        /// </summary>
        public bool AbilityArrow;

        public void CopyFrom(TipDef o)
        {
            Tip = o.Tip;
            Element = o.Element;
            Ammo = o.Ammo;
            Damage = o.Damage;
            GravityScale = o.GravityScale;
            BurnPerTurn = o.BurnPerTurn;
            BurnTurns = o.BurnTurns;
            PoisonPerTurn = o.PoisonPerTurn;
            PoisonTurns = o.PoisonTurns;
            ChainFraction = o.ChainFraction;
            ChainRadius = o.ChainRadius;
            SplashDamage = o.SplashDamage;
            SplashRadius = o.SplashRadius;
            SplitCount = o.SplitCount;
            SplitSpreadDeg = o.SplitSpreadDeg;
            StunSeconds = o.StunSeconds;
            FreezeDrawSlow = o.FreezeDrawSlow;
            PopsBubbles = o.PopsBubbles;
            KnocksShields = o.KnocksShields;
            Knockback = o.Knockback;
            LaunchCount = o.LaunchCount;
            LaunchSpreadDeg = o.LaunchSpreadDeg;
            SpeedScale = o.SpeedScale;
            WindScale = o.WindScale;
            ChainCount = o.ChainCount;
            AbilityArrow = o.AbilityArrow;
        }
    }
}
