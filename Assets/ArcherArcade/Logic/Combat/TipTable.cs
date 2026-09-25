namespace ArcherArcade.Logic
{
    /// <summary>All arrow tips, indexed by <see cref="ArrowTip"/>.</summary>
    public sealed class TipTable
    {
        public const int Count = 8;

        readonly TipDef[] _defs = new TipDef[Count];

        public TipDef this[ArrowTip tip] => _defs[(int)tip];

        public void Set(TipDef def) => _defs[(int)def.Tip] = def;

        /// <summary>Starting values from GAME_DESIGN §5 and §3.3.1.</summary>
        public static TipTable CreateDefault()
        {
            var t = new TipTable();
            t.Set(new TipDef { Tip = ArrowTip.Normal, Element = Element.None, Ammo = -1, Damage = 25 });
            t.Set(new TipDef { Tip = ArrowTip.Fire, Element = Element.Fire, Ammo = 3, Damage = 22, BurnPerTurn = 6, BurnTurns = 2 });
            t.Set(new TipDef
            {
                Tip = ArrowTip.Electric, Element = Element.Electric, Ammo = 3, Damage = 20,
                ChainFraction = 0.5, ChainRadius = 3.0, StunSeconds = 4.0, PopsBubbles = true
            });
            t.Set(new TipDef { Tip = ArrowTip.Split, Element = Element.None, Ammo = 2, Damage = 12, SplitCount = 3, SplitSpreadDeg = 6.0 });
            t.Set(new TipDef { Tip = ArrowTip.Bomb, Element = Element.Bomb, Ammo = 2, Damage = 20, SplashDamage = 12, SplashRadius = 1.2,
                KnocksShields = true, Knockback = true });
            t.Set(new TipDef { Tip = ArrowTip.Heavy, Element = Element.None, Ammo = 3, Damage = 35, GravityScale = 1.35, KnocksShields = true });
            t.Set(new TipDef { Tip = ArrowTip.Ice, Element = Element.Ice, Ammo = 2, Damage = 18, FreezeDrawSlow = 0.3 });
            t.Set(new TipDef { Tip = ArrowTip.Poison, Element = Element.Poison, Ammo = 2, Damage = 10, PoisonPerTurn = 8, PoisonTurns = 2 });
            return t;
        }
    }
}
