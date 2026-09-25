namespace ArcherArcade.Logic
{
    /// <summary>Live state of one archer in a match.</summary>
    public sealed class Fighter
    {
        public readonly int Index;
        public readonly FighterSpec Spec;
        public readonly int MaxHp;
        public int Hp;
        public StatusEffects Status;

        /// <summary>Ammo per tip (index = (int)ArrowTip); −1 = unlimited, 0 = none.</summary>
        public readonly int[] Ammo = new int[TipTable.Count];

        public Fighter(int index, FighterSpec spec, TipTable tips, DamageConfig damage)
        {
            Index = index;
            Spec = spec;
            int baseHp = Damage.MaxHp(spec.Def, spec.Level, damage);
            MaxHp = DetMath.RoundToInt(baseHp * spec.HpScale) + spec.BonusHp;
            if (MaxHp < 1) MaxHp = 1;
            Hp = MaxHp;
            Ammo[(int)ArrowTip.Normal] = -1;
            for (int i = 0; spec.Tips != null && i < spec.Tips.Length; i++)
            {
                ArrowTip tip = spec.Tips[i];
                if (tip == ArrowTip.Normal) continue;
                Ammo[(int)tip] = tips[tip].Ammo;
            }
        }

        public ArcherDef Def => Spec.Def;
        public int Side => Spec.Side;
        public int Level => Spec.Level;
        public Vec2 Feet => Spec.Feet;
        public int Facing => Spec.Facing;
        public bool IsAlive => Hp > 0;
        public double HpFraction => (double)Hp / MaxHp;

        public Vec2 BowPosition(ShotConfig cfg) => Feet + new Vec2(Facing * cfg.BowForward, cfg.BowHeight);

        public bool HasAmmo(ArrowTip tip) => Ammo[(int)tip] != 0;
    }
}
