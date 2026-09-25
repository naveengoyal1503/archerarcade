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

        /// <summary>Where the archer stands now (moves with platforms, towers and knockback).</summary>
        public Vec2 Feet;

        /// <summary>Ammo per tip (index = (int)ArrowTip); −1 = unlimited, 0 = none.</summary>
        public readonly int[] Ammo = new int[TipTable.Count];

        /// <summary>Own turns finished since the ability was last used, and how many it needs now.</summary>
        public int AbilityCharge;
        public int AbilityChargeNeeded;

        /// <summary>Turns this archer has started (drives healer / bubble cadence).</summary>
        public int OwnTurns;

        /// <summary>Landed a headshot during the current turn (speeds up the ability charge).</summary>
        public bool HeadshotThisTurn;

        /// <summary>Used the ability this turn (that turn does not count toward the next charge).</summary>
        public bool AbilityUsedThisTurn;

        /// <summary>A shield bubble absorbs the next arrow that hits (Shield Bubble booster, bubble casters).</summary>
        public bool HasBubble;

        /// <summary>Iron Helmet booster: the first headshot on this archer counts as a body hit.</summary>
        public bool HelmetLeft;

        /// <summary>Multi Arrow booster: the first shot fans out.</summary>
        public bool MultiArrowLeft;

        /// <summary>Element marks shown on the rig until the match ends.</summary>
        public ElementMarks Marks;

        /// <summary>Boss enrage reached (shoots twice per turn).</summary>
        public bool Enraged;

        public Fighter(int index, FighterSpec spec, TipTable tips, DamageConfig damage, BoosterConfig boosters,
            MatchConfig rules)
        {
            Index = index;
            Spec = spec;
            int baseHp = Damage.MaxHp(spec.Def, spec.Level, damage);
            MaxHp = DetMath.RoundToInt(baseHp * spec.HpScale) + spec.BonusHp;
            if (HasBooster(BoosterKind.ExtraHeart)) MaxHp += boosters.ExtraHeartHp;
            if (MaxHp < 1) MaxHp = 1;
            Hp = spec.StartHp > 0 && spec.StartHp < MaxHp ? spec.StartHp : MaxHp;
            Feet = spec.Feet;
            Ammo[(int)ArrowTip.Normal] = spec.NormalArrows ? -1 : 0;
            for (int i = 0; spec.Tips != null && i < spec.Tips.Length; i++)
            {
                ArrowTip tip = spec.Tips[i];
                if (tip == ArrowTip.Normal) continue;
                Ammo[(int)tip] = tips[tip].Ammo;
            }
            AbilityChargeNeeded = rules.AbilityChargeTurns;
            HasBubble = HasBooster(BoosterKind.ShieldBubble);
            HelmetLeft = HasBooster(BoosterKind.IronHelmet);
            MultiArrowLeft = HasBooster(BoosterKind.MultiArrow);
        }

        public ArcherDef Def => Spec.Def;
        public int Side => Spec.Side;
        public int Level => Spec.Level;
        public int Facing => Spec.Facing;
        public bool IsAlive => Hp > 0;
        public double HpFraction => (double)Hp / MaxHp;
        public InjuryStage Injury => ArcherArcade.Logic.Injury.Stage(Hp, MaxHp);

        public bool HasAbility => Spec.Def.Ability != AbilityKind.None;
        public bool AbilityReady => HasAbility && AbilityCharge >= AbilityChargeNeeded;

        public Vec2 BowPosition(ShotConfig cfg) => Feet + new Vec2(Facing * cfg.BowForward, cfg.BowHeight);

        public bool HasAmmo(ArrowTip tip) => Ammo[(int)tip] != 0;

        public bool HasBooster(BoosterKind kind)
        {
            BoosterKind[] b = Spec.Boosters;
            for (int i = 0; b != null && i < b.Length; i++)
            {
                if (b[i] == kind) return true;
            }
            return false;
        }
    }
}
