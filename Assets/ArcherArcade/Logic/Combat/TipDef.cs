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
    }
}
