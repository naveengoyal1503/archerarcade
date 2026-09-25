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
        MatchOver,

        // Props (Phase 4)
        PropHit,
        CrateBroken,
        CrateKnockedOff,
        TowerToppled,
        Explosion,
        ExplosionHit,
        Bounce,
        TargetHit,
        AppleHit,
        DummyHit,
        RopeCut,
        ShieldBlocked,
        ShieldKnockedDown,
        ShieldRaised,
        PlatformMoved,
        FighterDropped,
        Knockback,
        Stumble,

        // Archers, abilities, boosters, enemy behaviour (Phase 7)
        AbilityUsed,
        AbilityReady,
        BoosterUsed,
        ExtraShot,
        BubbleCast,
        BubbleAbsorbed,
        BubblePopped,
        HelmetSaved,
        Healed,

        // Bosses (Phase 8)
        ShieldsRotated,
        VineWallGrown,
        VineWallBroken,
        Enraged,
        RainOfLeaves
    }
}
