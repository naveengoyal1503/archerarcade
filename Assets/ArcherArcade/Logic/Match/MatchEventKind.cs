namespace ArcherArcade.Logic
{
    public enum MatchEventKind
    {
        TurnStarted,
        WindChanged,
        BurnDamage,
        PoisonDamage,
        Hit,
        ChainHit,
        SplashHit,
        Miss,
        BurnApplied,
        PoisonApplied,
        Stunned,
        Frozen,
        Knockout,
        FighterEntered,
        TurnTimedOut,
        MatchOver
    }
}
