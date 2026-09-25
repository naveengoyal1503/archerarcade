namespace ArcherArcade.Logic
{
    /// <summary>All abilities, indexed by <see cref="AbilityKind"/>. Starting values from GAME_DESIGN §4.</summary>
    public sealed class AbilityTable
    {
        const int Count = 5;
        readonly AbilityDef[] _defs = new AbilityDef[Count];

        public AbilityDef this[AbilityKind kind] => _defs[(int)kind];

        public void Set(AbilityDef def) => _defs[(int)def.Kind] = def;

        public static AbilityTable CreateDefault()
        {
            var t = new AbilityTable();
            t.Set(new AbilityDef { Kind = AbilityKind.None });

            // Ranger: 3 arrows in a ±4° spread, 60 % damage each (uses the picked tip).
            t.Set(new AbilityDef { Kind = AbilityKind.TripleShot, LaunchCount = 3, LaunchSpreadDeg = 4.0, DamageScale = 0.6 });

            // Fire Archer: flaming arrow, 40 damage + burn, 1.5 m splash (12).
            t.Set(new AbilityDef
            {
                Kind = AbilityKind.MeteorArrow,
                Arrow = new TipDef
                {
                    Tip = ArrowTip.Fire, Element = Element.Fire, Damage = 40, BurnPerTurn = 6, BurnTurns = 2,
                    SplashDamage = 12, SplashRadius = 1.5, KnocksShields = true, AbilityArrow = true
                }
            });

            // Electric Archer: fast bolt that ignores wind, 35 damage, chains twice.
            t.Set(new AbilityDef
            {
                Kind = AbilityKind.StormBolt,
                Arrow = new TipDef
                {
                    Tip = ArrowTip.Electric, Element = Element.Electric, Damage = 35, SpeedScale = 1.25, WindScale = 0.0,
                    ChainFraction = 0.5, ChainRadius = 3.0, ChainCount = 2, PopsBubbles = true, AbilityArrow = true
                }
            });

            // Bomb Archer: splits into 3 bomblets at the apex, 18 each (+ the Bomb Archer's own splash).
            t.Set(new AbilityDef
            {
                Kind = AbilityKind.ClusterBomb,
                Arrow = new TipDef
                {
                    Tip = ArrowTip.Bomb, Element = Element.Bomb, Damage = 18, SplitCount = 3, SplitSpreadDeg = 8.0,
                    KnocksShields = true, AbilityArrow = true
                }
            });
            return t;
        }
    }
}
