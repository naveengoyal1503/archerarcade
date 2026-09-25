namespace ArcherArcade.Logic
{
    /// <summary>Live state of one prop in a match.</summary>
    public sealed class Prop
    {
        public readonly int Index;
        public readonly PropSpec Spec;
        public int HitsLeft;
        public bool Alive = true;

        /// <summary>Tower crates drop down when crates below are knocked off.</summary>
        public Vec2 StackOffset;

        /// <summary>Moving platforms: where the platform is this turn.</summary>
        public Vec2 TurnOffset;

        /// <summary>Shield knocked down on this turn number (0 = standing).</summary>
        public int ShieldDownTurn;

        public Prop(int index, PropSpec spec, PropConfig cfg)
        {
            Index = index;
            Spec = spec;
            HitsLeft = spec.Hits > 0 ? spec.Hits : (spec.Kind == PropKind.Crate ? cfg.CrateHits : 1);
        }

        public PropKind Kind => Spec.Kind;
        public bool ShieldDown => ShieldDownTurn > 0;

        /// <summary>Can it be damaged, triggered or chained to?</summary>
        public bool IsBreakable
        {
            get
            {
                switch (Spec.Kind)
                {
                    case PropKind.Crate:
                    case PropKind.TntCrate:
                    case PropKind.ExplosiveBarrel:
                    case PropKind.Target:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
