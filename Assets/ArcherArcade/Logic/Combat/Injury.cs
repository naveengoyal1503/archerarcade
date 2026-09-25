namespace ArcherArcade.Logic
{
    public static class Injury
    {
        /// <summary>Stage from HP left, in whole percent of max HP (76 % is still Clean, 75 % is Scratched).</summary>
        public static InjuryStage Stage(int hp, int maxHp)
        {
            if (hp <= 0 || maxHp <= 0) return InjuryStage.KnockedOut;
            // Compare hp/max against 3/4, 1/2, 1/4 in integers: no rounding surprises.
            if (hp * 4 > maxHp * 3) return InjuryStage.Clean;
            if (hp * 2 > maxHp) return InjuryStage.Scratched;
            if (hp * 4 > maxHp) return InjuryStage.Limping;
            return InjuryStage.Dizzy;
        }

        public static ElementMarks MarkFor(Element element)
        {
            switch (element)
            {
                case Element.Fire: return ElementMarks.Soot;
                case Element.Electric: return ElementMarks.Sparks;
                case Element.Ice: return ElementMarks.Frost;
                case Element.Poison: return ElementMarks.Poisoned;
                case Element.Bomb: return ElementMarks.Bruised;
                default: return ElementMarks.None;
            }
        }
    }
}
